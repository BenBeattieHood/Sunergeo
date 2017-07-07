namespace Sunergeo.Projection.Default

open Sunergeo.Core
open Sunergeo.Logging
open Sunergeo.KeyValueStorage

type KeyValueStorageProjectionConfig<'PartitionId, 'Events, 'State when 'PartitionId : comparison> = {
    Logger: Logger
    KeyValueStorageConfig: Sunergeo.KeyValueStorage.KeyValueStorageConfig
    PartitionId: 'PartitionId
    CreateState: EventSourceInitItem<'PartitionId> -> 'State
    FoldState: 'State -> 'Events -> 'State
}
type KeyValueStoreProjector<'PartitionId, 'Events, 'State when 'PartitionId : comparison>(config: KeyValueStorageProjectionConfig<'PartitionId, 'Events, 'State>) =
    inherit Sunergeo.Projection.Projector<'PartitionId, 'Events>()

    let keyValueStore = new KeyValueStore<'PartitionId, 'State>(config.KeyValueStorageConfig)

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


    override this.Process(eventLogItem):unit =
        match eventLogItem with
        | EventLogItem.Init init ->
            // TODO - check state doesn't already exist? Maybe silent failure is better in this case

            let newState = 
                init
                |> config.CreateState
            
            let writeResult =
                keyValueStore.Create
                    config.PartitionId
                    newState

            writeResult |> processWriteResult

        | EventLogItem.Event event ->
            let state =
                keyValueStore.Get config.PartitionId

            match state with
            | Ok (Some (state, version)) ->
                
                let newState =
                    event
                    |> config.FoldState state

                let writeResult = 
                    keyValueStore.Put
                        config.PartitionId
                        (newState, version)
                        
                writeResult |> processWriteResult

            | Ok None ->
                sprintf "No state found for event %O" event
                |> config.Logger LogLevel.Error

            | Result.Error error ->
                match error with
                | ReadError.Timeout -> 
                    "KeyValueStore timeout"
                | ReadError.Error error ->
                    error

                |> config.Logger LogLevel.Error

    interface System.IDisposable with
        member this.Dispose() =
            (keyValueStore :> System.IDisposable).Dispose()