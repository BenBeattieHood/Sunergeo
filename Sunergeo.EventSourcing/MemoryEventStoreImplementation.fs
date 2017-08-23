namespace Sunergeo.EventSourcing.Memory

open Sunergeo.EventSourcing.Storage

open System
open Sunergeo.Core
open Sunergeo.KeyValueStorage
open Sunergeo.EventSourcing.Storage

type LogEntry<'Item> = {
    Position: ShardPartitionPosition
    Item: 'Item
}

type MemoryLogTransactionId = Guid

type MemoryLogTopic<'AggregateId, 'Item when 'AggregateId : comparison>() =
    
    let mutable eventSource:Map<'AggregateId, LogEntry<'Item> seq> = Map.empty
    let mutable transactions:Map<MemoryLogTransactionId, ('AggregateId * 'Item) seq> = Map.empty

    member this.BeginTransaction(): MemoryLogTransactionId =
        Guid.NewGuid().ToByteArray() |> MemoryLogTransactionId

    member this.AbortTransaction(transactionId: MemoryLogTransactionId): unit =
        lock transactions
            (fun _ ->
                transactions <- 
                    transactions 
                    |> Map.remove
                        transactionId
            )

    member this.CommitTransaction(transactionId: MemoryLogTransactionId): unit =
        lock transactions
            (fun _ ->
                let partitionItems =
                    transactions
                    |> Map.tryFind transactionId
                    |> Option.defaultValue Seq.empty
                    |> Seq.groupBy
                        (fun (aggregateId, _) -> aggregateId)
                    |> Seq.map
                        (fun (a, items) -> a, items |> Seq.map snd) /// there should be a Seq.groupBy alternative for this

                lock eventSource
                    (fun _ ->
                        for (aggregateId, items) in partitionItems do

                            let partition =
                                eventSource
                                |> Map.tryFind aggregateId
                                |> Option.defaultValue Seq.empty
                
                            let partitionLength = 
                                partition
                                |> Seq.length
            
                            for index, item in items |> Seq.indexed do
                                eventSource <- 
                                    eventSource 
                                    |> Map.add 
                                        aggregateId 
                                        (
                                            partition 
                                            |> Seq.append [ { Position = partitionLength + index |> int64; Item = item } ]
                                        )
                    )
            )

    member this.Add
        (transactionId: MemoryLogTransactionId)
        (aggregateId: 'AggregateId)
        (item: 'Item)
        : unit =

        lock transactions
            (fun _ ->
                let newPartitionsAndItems =
                    transactions
                    |> Map.tryFind transactionId
                    |> Option.defaultValue Seq.empty
                    |> Seq.append [ (aggregateId, item) ]

                transactions <-
                    transactions
                    |> Map.add transactionId newPartitionsAndItems
            )

    member this.GetPositions(): Map<'AggregateId, ShardPartitionPosition> =
        eventSource
        |> Map.map
            (fun aggregateId items ->
                (items |> Seq.head).Position
            )

    member this.ReadFrom
        (aggregateId: 'AggregateId)
        (position: int)
        : LogEntry<'Item> seq option =

        eventSource
        |> Map.tryFind aggregateId

type MemoryEventStoreImplementationConfig<'AggregateId, 'State, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = {
    ShardId: ShardId
    Logger: Sunergeo.Logging.Logger
    SnapshotStore: IKeyValueStore<'AggregateId, Snapshot<'State>, 'KeyValueVersion>
}

type IEventSource<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> =
    abstract member GetPositions: unit -> Async<Map<'AggregateId, int>>
    abstract member ReadFrom: 'AggregateId -> int -> Async<LogEntry<EventLogItem<'AggregateId, 'Init, 'Events>> seq option>

type MemoryEventStoreImplementation<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: MemoryEventStoreImplementationConfig<'AggregateId, 'State, 'KeyValueVersion>) = 
    let memoryTopic = new MemoryLogTopic<'AggregateId, EventLogItem<'AggregateId, 'Init, 'Events>>()
    
    interface IEventSource<'AggregateId, 'Init, 'Events> with
        member this.GetPositions () =
            async { return memoryTopic.GetPositions() }

        member this.ReadFrom aggregateId position =
            async { return memoryTopic.ReadFrom aggregateId position }

    interface IEventStoreImplementation<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion> with

        member this.Append aggregateId getNewStateAndEvents =
            async {
                let snapshotAndVersion = 
                    aggregateId 
                    |> config.SnapshotStore.Get 
                    |> ResultModule.get 

                let (newState, events, version) = 
                    snapshotAndVersion 
                    |> getNewStateAndEvents
                    |> ResultModule.get
            
                let transactionId = memoryTopic.BeginTransaction()
            
                try
                    for event in events do
                        memoryTopic.Add transactionId aggregateId event
                    
                    let shardPartitionPosition = events |> Seq.length |> int64

                    let snapshot = {
                        Snapshot.ShardPartition = 
                            {
                                ShardPartition.ShardId = config.ShardId
                                ShardPartition.ShardPartitionId = 0
                            }
                        Snapshot.ShardPartitionPosition = shardPartitionPosition
                        Snapshot.State = newState
                    }

                    let snapshotPutResult =
                        match version with
                        | None ->
                            config.SnapshotStore.Create
                                aggregateId
                                snapshot

                        | Some version ->
                            config.SnapshotStore.Put
                                aggregateId
                                (snapshot, version)

                    do snapshotPutResult |> ResultModule.get
                
                    do memoryTopic.CommitTransaction(transactionId)

                    return () |> Result.Ok
                with
                    //| :? ResultModule.ResultException<Sunergeo.EventSourcing.Storage.LogError> as resultException ->
                    //    do! memoryTopic.AbortTransaction()
                    //    return 
                    //        match resultException.Error with
                    //        | Timeout ->
                    //            Error. Result.Error

                    | :? ResultModule.ResultException<Sunergeo.KeyValueStorage.WriteError> as resultException ->
                        do memoryTopic.AbortTransaction(transactionId)

                        return 
                            match resultException.Error with
                            | Sunergeo.KeyValueStorage.WriteError.Timeout -> 
                                Sunergeo.Core.Todo.todo()

                            | Sunergeo.KeyValueStorage.WriteError.InvalidVersion -> 
                                Sunergeo.Core.Todo.todo()

                            | Sunergeo.KeyValueStorage.WriteError.Error error -> 
                                error 
                                |> Sunergeo.Core.Error.InvalidOp 
                                |> Result.Error

                    | _ as ex ->
                        do memoryTopic.AbortTransaction(transactionId)
                        return Sunergeo.Core.NotImplemented.NotImplemented() //This needs transaction support

            }