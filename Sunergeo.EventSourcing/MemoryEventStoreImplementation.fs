namespace Sunergeo.EventSourcing.Memory

open Sunergeo.EventSourcing.Storage

open System
open Sunergeo.Core
open Sunergeo.KeyValueStorage
open Sunergeo.EventSourcing.Storage

type MemoryLogConfig = {
    Uri: string // placeholder
    Topic: string
}

type MemoryLogTopic<'PartitionId, 'Item when 'PartitionId : comparison>(config: MemoryLogConfig) =
    
    let mutable eventSource:Map<'PartitionId, LogEntry<'Item> seq> = Map.empty

    member this.BeginTransaction(): Async<LogTransactionId> =
        async {
            return ("" : LogTransactionId)
        }

    member this.AbortTransaction(): Async<unit> =
        async {
            return Sunergeo.Core.Todo.todo()
        }

    member this.CommitTransaction(): Async<unit> =
        async {
            return Sunergeo.Core.Todo.todo()
        }

    member this.Add(partitionId: 'PartitionId, item: 'Item): Async<Result<int, LogError>> =
        async {
            let partition =
                eventSource
                |> Map.tryFind partitionId
                |> Option.defaultValue Seq.empty

            let partitionLength = 
                partition
                |> Seq.length
            
            eventSource <- 
                eventSource 
                |> Map.add 
                    partitionId 
                    (
                        partition 
                        |> Seq.append [ { Position = partitionLength; Item = item } ]
                    )

            return partitionLength |> Result.Ok
        }

type MemoryEventStoreImplementationConfig<'PartitionId, 'State, 'Events when 'PartitionId : comparison> = {
    InstanceId: InstanceId
    Logger: Sunergeo.Logging.Logger
    Implementation: IEventStoreImplementation<'PartitionId, 'State, 'Events>
    SnapshotStore: Sunergeo.KeyValueStorage.KeyValueStore<'PartitionId, Snapshot<'State>>
    LogUri: Uri
}

type MemoryEventStoreImplementation<'PartitionId, 'State, 'Events when 'PartitionId : comparison>(config: MemoryEventStoreImplementationConfig<'PartitionId, 'State, 'Events>) = 
    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>
        
    let logConfig:MemoryLogConfig = {
        Topic = topic
        Uri = config.LogUri
    }

    let memoryTopic = new MemoryLogTopic<'PartitionId, EventLogItem<'PartitionId, 'Events>>(logConfig)
    
    let append
        (partitionId: 'PartitionId)
        (getNewStateAndEvents: (Snapshot<'State> * int) option -> Result<'State * (EventLogItem<'PartitionId, 'Events> seq) * (int option), Error>)
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
            
            let! transactionId = memoryTopic.BeginTransaction()
            
            try
                let mutable position = 0 |> int64
                
                for event in events do
                    let! positionResult = memoryTopic.Add(partitionId, event)
                    position <- (positionResult |> ResultModule.get)    
                    
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
                
                do! memoryTopic.CommitTransaction()

                return () |> Result.Ok
            with
                //| :? ResultModule.ResultException<Sunergeo.EventSourcing.Storage.LogError> as resultException ->
                //    do! memoryTopic.AbortTransaction()
                //    return 
                //        match resultException.Error with
                //        | Timeout ->
                //            Error. Result.Error

                | :? ResultModule.ResultException<Sunergeo.KeyValueStorage.WriteError> as resultException ->
                    do! memoryTopic.AbortTransaction()

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

                | :? _ as ex ->
                    do! memoryTopic.AbortTransaction()
                    return Sunergeo.Core.NotImplemented.NotImplemented() //This needs transaction support

        }