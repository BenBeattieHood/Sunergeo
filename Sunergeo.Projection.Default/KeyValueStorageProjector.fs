namespace Sunergeo.Projection.Default

open Sunergeo.Core
open Sunergeo.Logging
open Sunergeo.KeyValueStorage

type KeyValueStorageProjectionConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = {
    Logger: Logger
    CreateState: EventSourceInitItem<'AggregateId, 'Init> -> 'State
    FoldState: 'State -> 'Events -> 'State
    KeyValueStore: IKeyValueStore<'AggregateId, 'State, 'KeyValueVersion>
}
type KeyValueStoreProjector<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: KeyValueStorageProjectionConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>, aggregateId: 'AggregateId) =
    inherit Sunergeo.Projection.Projector<'AggregateId, 'Init, 'Events>()

    let processWriteResult 
        (writeResult: Result<unit, WriteError>)
        : unit =
        match writeResult with
        | Ok unit -> unit
        | Result.Error error -> 
            match error with
            | WriteError.Timeout -> "KeyValueStore timeout"
            | WriteError.InvalidVersion -> "KeyValueStore invalid version"
            | WriteError.Error error -> error
            |> config.Logger LogLevel.Error
            
    let processWithState 
        (f: Option<'State * 'KeyValueVersion> -> unit)
        : unit =
        match config.KeyValueStore.Get aggregateId with
        | Ok x -> f x
        | Result.Error error ->
            match error with
            | ReadError.Timeout -> "KeyValueStore timeout"
            | ReadError.Error error -> error
            |> config.Logger LogLevel.Error
            
    override this.Process(eventLogItem:EventLogItem<'AggregateId, 'Init, 'Events>):unit =

        match eventLogItem with
        | EventLogItem.Init init ->
            (function
            | Some state ->
                sprintf "State already present for init %O %O" init state
                |> config.Logger LogLevel.Error

            | None ->
                let newState = 
                    init
                    |> config.CreateState
            
                let writeResult =
                    config.KeyValueStore.Create
                        aggregateId
                        newState

                writeResult |> processWriteResult
            )
            |> processWithState

        | EventLogItem.Event event ->
        
            (function
            | None ->
                sprintf "No state found for event %O" event
                |> config.Logger LogLevel.Error
                
            | Some (state, version) ->
                let newState =
                    event
                    |> config.FoldState state

                let writeResult = 
                    config.KeyValueStore.Put
                        aggregateId
                        (newState, version)
                        
                writeResult |> processWriteResult
            )
            |> processWithState


type KeyValueStoreProjectorHost<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: Sunergeo.Projection.ProjectionHostConfig<KeyValueStorageProjectionConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>, 'AggregateId>) =
    inherit Sunergeo.Projection.ProjectionHost<KeyValueStorageProjectionConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>, 'AggregateId, 'Init, 'State, 'Events>(config)
    override this.CreateActor config aggregateId = upcast new KeyValueStoreProjector<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>(config, aggregateId)