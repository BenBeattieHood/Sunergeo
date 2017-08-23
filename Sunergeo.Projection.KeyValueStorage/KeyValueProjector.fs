namespace Sunergeo.Projection.KeyValueStorage

open Sunergeo.Core
open Sunergeo.Projection
open Sunergeo.Logging
open Sunergeo.KeyValueStorage
open Sunergeo.EventSourcing.Storage

open System

type KeyValueProjectorConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = {
    Logger: Logger
    KeyValueStore: IKeyValueStore<'AggregateId, Snapshot<'State>, 'KeyValueVersion>
    CreateState: 'AggregateId -> 'Init -> 'State
    Fold: 'State -> 'Events -> 'State
}

type KeyValueProjector<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: KeyValueProjectorConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>) = 
    member this.Project (shardPartition: ShardPartition) (shardPartitionPosition: ShardPartitionPosition) (item: EventLogItem<'AggregateId, 'Init, 'Events>): Async<Result<unit, Error>> =
        async {
            try
                let aggregateId = item.Metadata.AggregateId

                let snapshotAndVersion = 
                    aggregateId 
                    |> config.KeyValueStore.Get 
                    |> ResultModule.get
                    
                match snapshotAndVersion, item.Data with
                | None, EventLogItemData.Event event ->
                    sprintf "No state exists" |> WriteError.Error |> Result.Error

                | Some (state, version), EventLogItemData.Init init ->
                    sprintf "State already exists" |> WriteError.Error |> Result.Error

                | None, EventLogItemData.Init init ->
                    let newState = config.CreateState aggregateId init
                    config.KeyValueStore.Create
                        aggregateId
                        newState

                | Some (state, version), EventLogItemData.Event event ->
                    if state.
                    let newState = config.Fold state event
                    config.KeyValueStore.Put
                        aggregateId
                        (newState, version)

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
