namespace Sunergeo.Projection.KeyValueStorage

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

module Implementation =
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
                    elif snapshot.ShardPartition = shardPartition && snapshot.ShardPartitionPosition >= shardPartitionPosition
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
                    elif snapshot.ShardPartition = shardPartition && snapshot.ShardPartitionPosition >= shardPartitionPosition
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


    //type KeyValueStorageProjectionConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = {
    //    Logger: Logger
    //    CreateState: EventSourceInitItem<'AggregateId, 'Init> -> 'State
    //    FoldState: 'State -> 'Events -> 'State
    //    KeyValueStore: IKeyValueStore<'AggregateId, 'State, 'KeyValueVersion>
    //}
    //type KeyValueStoreProjector<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: KeyValueStorageProjectionConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>, aggregateId: 'AggregateId) =
    //    inherit Sunergeo.Projection.Projector<'AggregateId, 'Init, 'Events>()

    //    let processWriteResult 
    //        (writeResult: Result<unit, WriteError>)
    //        : unit =
    //        match writeResult with
    //        | Ok unit -> unit
    //        | Result.Error error -> 
    //            match error with
    //            | WriteError.Timeout -> "KeyValueStore timeout"
    //            | WriteError.InvalidVersion -> "KeyValueStore invalid version"
    //            | WriteError.Error error -> error
    //            |> config.Logger LogLevel.Error
            
    //    let processWithState 
    //        (f: Option<'State * 'KeyValueVersion> -> unit)
    //        : unit =
    //        match config.KeyValueStore.Get aggregateId with
    //        | Ok x -> f x
    //        | Result.Error error ->
    //            match error with
    //            | ReadError.Timeout -> "KeyValueStore timeout"
    //            | ReadError.Error error -> error
    //            |> config.Logger LogLevel.Error
            
    //    override this.Process(eventLogItem:EventLogItem<'AggregateId, 'Init, 'Events>):unit =

    //        match eventLogItem with
    //        | EventLogItem.Init init ->
    //            (function
    //            | Some state ->
    //                sprintf "State already present for init %O %O" init state
    //                |> config.Logger LogLevel.Error

    //            | None ->
    //                let newState = 
    //                    init
    //                    |> config.CreateState
            
    //                let writeResult =
    //                    config.KeyValueStore.Create
    //                        aggregateId
    //                        newState

    //                writeResult |> processWriteResult
    //            )
    //            |> processWithState

    //        | EventLogItem.Event event ->
        
    //            (function
    //            | None ->
    //                sprintf "No state found for event %O" event
    //                |> config.Logger LogLevel.Error
                
    //            | Some (state, version) ->
    //                let newState =
    //                    event
    //                    |> config.FoldState state

    //                let writeResult = 
    //                    config.KeyValueStore.Put
    //                        aggregateId
    //                        (newState, version)
                        
    //                writeResult |> processWriteResult
    //            )
    //            |> processWithState


    //type KeyValueStoreProjectorHost<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion, 'PollingActor when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: Sunergeo.Projection.ProjectionHostConfig<KeyValueStorageProjectionConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>, 'AggregateId, 'Init, 'Events, 'PollingActor>) =
    //    inherit Sunergeo.Projection.ProjectionHost<KeyValueStorageProjectionConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>, 'AggregateId, 'Init, 'State, 'Events, 'PollingActor>(config)
    //    override this.CreateActor config aggregateId = upcast new KeyValueStoreProjector<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>(config, aggregateId)