namespace Sunergeo.EventSourcing.Kafka

open System
open Sunergeo.Core
open Sunergeo.KeyValueStorage
open Sunergeo.EventSourcing.Storage


type KafkaEventStoreImplementationConfig<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion when 'PartitionId : comparison and 'KeyValueVersion : comparison> = {
    InstanceId: InstanceId
    Logger: Sunergeo.Logging.Logger
    Implementation: IEventStoreImplementation<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion>
    SnapshotStore: Sunergeo.KeyValueStorage.IKeyValueStore<'PartitionId, Snapshot<'State>, 'KeyValueVersion>
    LogUri: Uri
}

type KafkaEventStoreImplementation<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion when 'PartitionId : comparison and 'KeyValueVersion : comparison>(config: KafkaEventStoreImplementationConfig<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion>) = 
    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>
        
    let logConfig:KafkaLogConfig = {
        Topic = topic
        Uri = config.LogUri
        Logger = config.Logger
    }

    let kafkaTopic = new KafkaLogTopic<'PartitionId, EventLogItem<'PartitionId, 'Init, 'Events>>(logConfig)
    
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
            
            let! transactionId = kafkaTopic.BeginTransaction()
            
            try
                let mutable position = 0 |> int64
                
                for event in events do
                    let! positionResult = kafkaTopic.Add(partitionId, event)
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
                
                do! kafkaTopic.CommitTransaction()

                return () |> Result.Ok
            with
                //| :? ResultModule.ResultException<Sunergeo.EventSourcing.Storage.LogError> as resultException ->
                //    do! kafkaTopic.AbortTransaction()
                //    return 
                //        match resultException.Error with
                //        | Timeout ->
                //            Error. Result.Error

                | :? ResultModule.ResultException<Sunergeo.KeyValueStorage.WriteError> as resultException ->
                    do! kafkaTopic.AbortTransaction()

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
                    do! kafkaTopic.AbortTransaction()
                    return Sunergeo.Core.NotImplemented.NotImplemented() //This needs transaction support

        }
        
    interface System.IDisposable with
        member this.Dispose() =
            (kafkaTopic :> IDisposable).Dispose()