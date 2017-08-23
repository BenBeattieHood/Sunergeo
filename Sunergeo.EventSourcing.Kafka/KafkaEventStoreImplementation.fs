namespace Sunergeo.EventSourcing.Kafka

open System
open Sunergeo.Core
open Sunergeo.KeyValueStorage
open Sunergeo.EventSourcing.Storage


type KafkaEventStoreImplementationConfig<'AggregateId, 'Metadata, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = {
    InstanceId: InstanceId
    Logger: Sunergeo.Logging.Logger
    Implementation: IEventStoreImplementation<'AggregateId, 'Metadata, 'Init, 'State, 'Events, 'KeyValueVersion>
    SnapshotStore: Sunergeo.KeyValueStorage.IKeyValueStore<'AggregateId, Snapshot<'State>, 'KeyValueVersion>
    LogUri: Uri
    ProducerConfig: Sunergeo.Kafka.KafkaProducerConfig
    SerializeAggregateId: 'AggregateId -> byte[]
    SerializeItem: EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events> -> byte[]
}

type KafkaEventStoreImplementation<'AggregateId, 'Metadata, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: KafkaEventStoreImplementationConfig<'AggregateId, 'Metadata, 'Init, 'State, 'Events, 'KeyValueVersion>) = 
    let shardId = 
        config.InstanceId 
        |> Utils.toShardId<'State>
        
    let logConfig:KafkaLogConfig<'AggregateId, EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events>> = 
        {
            KafkaLogConfig.ProducerConfig = config.ProducerConfig
            KafkaLogConfig.Topic = shardId
            KafkaLogConfig.Logger = config.Logger
            KafkaLogConfig.SerializeAggregateId = config.SerializeAggregateId
            KafkaLogConfig.SerializeItem = config.SerializeItem
        }

    let kafkaTopic = new KafkaLogTopic<'AggregateId, EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events>>(logConfig)
    
    interface IEventStoreImplementation<'AggregateId, 'Metadata, 'Init, 'State, 'Events, 'KeyValueVersion> with

        member this.Append aggregateId getNewStateAndEvents =
            async {
                try
                    let snapshotAndVersion = 
                        aggregateId 
                        |> config.SnapshotStore.Get 
                        |> ResultModule.get 

                    let (newState, events, version) = 
                        snapshotAndVersion 
                        |> getNewStateAndEvents
                        |> ResultModule.get
            
                    let! transactionId = kafkaTopic.BeginTransaction()

                    let mutable shardPartition =
                        snapshotAndVersion
                        |> Option.map
                            (fun (snapshot, version) ->
                                snapshot.ShardPartition
                            )
            
                    let mutable shardPartitionOffset = 0 |> int64
                
                    for event in events do
                        let! shardPartitionAndOffsetResult = kafkaTopic.Add(aggregateId, event)
                        let shardPartition', shardPartitionOffset' = shardPartitionAndOffsetResult |> ResultModule.get
                        match shardPartition with
                        | None ->
                            shardPartition <- Some shardPartition'
                        | Some x when x <> shardPartition' ->
                            raise (ResultModule.ResultException(sprintf "Wrote to incorrect shard: %O instead of %O" shardPartition' x |> Sunergeo.KeyValueStorage.WriteError.Error))
                        | _ ->
                            ()
                        shardPartitionOffset <- shardPartitionOffset'
                    
                    let snapshot = 
                        {
                            Snapshot.ShardPartition = shardPartition.Value
                            Snapshot.ShardPartitionOffset = shardPartitionOffset
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

                    | _ as ex ->
                        do! kafkaTopic.AbortTransaction()
                        return Sunergeo.Core.NotImplemented.NotImplemented() //This needs transaction support

            }
        
    interface System.IDisposable with
        member this.Dispose() =
            (kafkaTopic :> IDisposable).Dispose()