namespace Sunergeo.Projection.Default

open Sunergeo.Core
open Sunergeo.Logging
open Sunergeo.KeyValueStorage

type KeyValueStorageProjectionConfig<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion when 'PartitionId : comparison and 'KeyValueVersion : comparison> = {
    Logger: Logger
    CreateState: EventSourceInitItem<'PartitionId, 'Init> -> 'State
    FoldState: 'State -> 'Events -> 'State
    KeyValueStore: IKeyValueStore<'PartitionId, 'State, 'KeyValueVersion>
}
type KeyValueStoreProjector<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion when 'PartitionId : comparison and 'KeyValueVersion : comparison>(config: KeyValueStorageProjectionConfig<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion>, partitionId: 'PartitionId) =
    inherit Sunergeo.Projection.Projector<'PartitionId, 'Init, 'Events>()

    let processWriteResult 
        (writeResult: Result<unit, WriteError>)
        : unit =
        match writeResult with
        | Ok unit -> unit
        | Result.Error error -> 
            match error with
            | WriteError.Timeout -> 
                "KeyValueStore timeout"
            | WriteError.InvalidVersion ->
                "KeyValueStore invalid version"
            | WriteError.Error error ->
                error

            |> config.Logger LogLevel.Error
            
    let processWithState 
        (f: Option<'State * 'KeyValueVersion> -> unit)
        : unit =
        match config.KeyValueStore.Get partitionId with
        | Ok x -> f x
        | Result.Error error ->
            match error with
            | ReadError.Timeout -> 
                "KeyValueStore timeout"
            | ReadError.Error error ->
                error
            |> config.Logger LogLevel.Error
            
    override this.Process(eventLogItem:EventLogItem<'PartitionId, 'Init, 'Events>):unit =

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
                        partitionId
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
                        partitionId
                        (newState, version)
                        
                writeResult |> processWriteResult
            )
            |> processWithState


type KeyValueStoreProjectorHost<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion when 'PartitionId : comparison and 'KeyValueVersion : comparison>(config: Sunergeo.Projection.ProjectionHostConfig<KeyValueStorageProjectionConfig<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion>, 'PartitionId>) =
    inherit Sunergeo.Projection.ProjectionHost<KeyValueStorageProjectionConfig<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion>, 'PartitionId, 'Init, 'State, 'Events>(config)
    override this.CreateActor config partitionId = upcast new KeyValueStoreProjector<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion>(config, partitionId)