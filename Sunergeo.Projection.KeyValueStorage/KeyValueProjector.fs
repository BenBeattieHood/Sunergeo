module Sunergeo.Projection.KeyValueStorage

open Sunergeo.Core
open Sunergeo.Projection
open Sunergeo.Logging
open Sunergeo.KeyValueStorage

open System

type KeyValueProjectorConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = {
    Logger: Logger
    KeyValueStore: IKeyValueStore<'AggregateId, Snapshot<'State>, 'KeyValueVersion>
    CreateState: 'AggregateId -> 'Init -> 'State
    Fold: 'State -> 'Events -> 'State
}

let keyValueProjector<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>
    (config: KeyValueProjectorConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>)
    (shardPartition: ShardPartition)
    (shardPartitionPosition: ShardPartitionPosition)
    (item: EventLogItem<'AggregateId, 'Init, 'Events>)
    : Async<Result<unit, Error>> =

    async {
        try
            let aggregateId = item.Metadata.AggregateId

            let snapshotAndVersion = 
                aggregateId 
                |> config.KeyValueStore.Get 
                |> ResultModule.get
                    
            match snapshotAndVersion, item.Data with
            | None, EventLogItemData.Event event ->
                sprintf "No snapshot exists" |> WriteError.Error |> Result.Error

            | Some (snapshot, version), EventLogItemData.Init init ->
                if snapshot.ShardPartition <> shardPartition
                then
                    // replaying from a new partition, allow re-init
                    let newState = config.CreateState aggregateId init
                    config.KeyValueStore.Create
                        aggregateId
                        {
                            Snapshot.ShardPartition = shardPartition 
                            Snapshot.ShardPartitionPosition = shardPartitionPosition
                            Snapshot.State = newState
                        }
                elif snapshot.ShardPartition = shardPartition && snapshot.ShardPartitionPosition <= shardPartitionPosition
                then
                    // replaying partition, but this is already projected
                    () |> Result.Ok
                else
                    sprintf "Snapshot already exists" |> WriteError.Error |> Result.Error

            | None, EventLogItemData.Init init ->
                let newState = config.CreateState aggregateId init
                config.KeyValueStore.Create
                    aggregateId
                    {
                        Snapshot.ShardPartition = shardPartition 
                        Snapshot.ShardPartitionPosition = shardPartitionPosition
                        Snapshot.State = newState
                    }

            | Some (snapshot, version), EventLogItemData.Event event ->
                if snapshot.ShardPartition <> shardPartition
                then
                    sprintf "Partition has changed, projection needs to replay" |> WriteError.Error |> Result.Error
                elif snapshot.ShardPartition = shardPartition && snapshot.ShardPartitionPosition <= shardPartitionPosition
                then
                    // replaying partition, but this is already projected
                    () |> Result.Ok
                else
                    let newState = config.Fold snapshot.State event
                    config.KeyValueStore.Put
                        aggregateId
                        (
                            {
                                Snapshot.ShardPartition = shardPartition 
                                Snapshot.ShardPartitionPosition = shardPartitionPosition
                                Snapshot.State = newState
                            }, 
                            version
                        )

            |> ResultModule.get

            return () |> Result.Ok
        with
            | :? ResultModule.ResultException<Sunergeo.KeyValueStorage.WriteError> as resultException ->
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
    }
