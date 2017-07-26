namespace Sunergeo.EventSourcing.Memory

open Sunergeo.EventSourcing.Storage

open System
open Sunergeo.Core
open Sunergeo.KeyValueStorage
open Sunergeo.EventSourcing.Storage

type MemoryLogTransactionId = Guid

type MemoryLogTopic<'PartitionId, 'Item when 'PartitionId : comparison>() =
    
    let mutable eventSource:Map<'PartitionId, LogEntry<'Item> seq> = Map.empty
    let mutable transactions:Map<MemoryLogTransactionId, ('PartitionId * 'Item) seq> = Map.empty

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
                        (fun (partitionId, _) -> partitionId)
                    |> Seq.map
                        (fun (a, items) -> a, items |> Seq.map snd) /// there should be a Seq.groupBy alternative for this

                lock eventSource
                    (fun _ ->
                        for (partitionId, items) in partitionItems do

                            let partition =
                                eventSource
                                |> Map.tryFind partitionId
                                |> Option.defaultValue Seq.empty
                
                            let partitionLength = 
                                partition
                                |> Seq.length
            
                            for index, item in items |> Seq.indexed do
                                eventSource <- 
                                    eventSource 
                                    |> Map.add 
                                        partitionId 
                                        (
                                            partition 
                                            |> Seq.append [ { Position = partitionLength + index; Item = item } ]
                                        )
                    )
            )

    member this.Add(transactionId: MemoryLogTransactionId, partitionId: 'PartitionId, item: 'Item): unit =
        lock transactions
            (fun _ ->
                let newPartitionsAndItems =
                    transactions
                    |> Map.tryFind transactionId
                    |> Option.defaultValue Seq.empty
                    |> Seq.append [ (partitionId, item) ]

                transactions <-
                    transactions
                    |> Map.add transactionId newPartitionsAndItems
            )

type MemoryEventStoreImplementationConfig<'PartitionId, 'State, 'KeyValueVersion when 'PartitionId : comparison and 'KeyValueVersion : comparison> = {
    InstanceId: InstanceId
    Logger: Sunergeo.Logging.Logger
    SnapshotStore: Sunergeo.KeyValueStorage.IKeyValueStore<'PartitionId, Snapshot<'State>, 'KeyValueVersion>
}

type MemoryEventStoreImplementation<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion when 'PartitionId : comparison and 'KeyValueVersion : comparison>(config: MemoryEventStoreImplementationConfig<'PartitionId, 'State, 'KeyValueVersion>) = 
    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>

    let memoryTopic = new MemoryLogTopic<'PartitionId, EventLogItem<'PartitionId, 'Init, 'Events>>()
    
    let append
        (partitionId: 'PartitionId)
        (getNewStateAndEvents: (Snapshot<'State> * 'KeyValueVersion) option -> Result<'State * (EventLogItem<'PartitionId, 'Init, 'Events> seq) * ('KeyValueVersion option), Error>)
        :Async<Result<unit, Error>> =
        
        async {
            let snapshotAndVersion = 
                partitionId 
                |> config.SnapshotStore.Get 

            let (newState, events, version) = 
                snapshotAndVersion 
                |> ResultModule.get 
                |> getNewStateAndEvents
                |> ResultModule.get
            
            let transactionId = memoryTopic.BeginTransaction()
            
            try
                for event in events do
                    memoryTopic.Add(transactionId, partitionId, event)
                    
                let position = events |> Seq.length |> int64

                let snapshot = {
                    Snapshot.Position = position
                    Snapshot.State = newState
                }

                let snapshotPutResult =
                    match version with
                    | None ->
                        config.SnapshotStore.Create
                            partitionId
                            snapshot

                    | Some version ->
                        config.SnapshotStore.Put
                            partitionId
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