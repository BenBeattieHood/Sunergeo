namespace Sunergeo.Projection.KeyValueStorage

open Sunergeo.Core
open Sunergeo.Projection
open Sunergeo.Logging
open Sunergeo.KeyValueStorage

open System

type KeyValueProjectorConfig<'AggregateId, 'Metadata, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = {
    Logger: Logger
    KeyValueStore: IKeyValueStore<'AggregateId, 'State, 'KeyValueVersion>
    Projector: 'State -> EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events> 
}
type KeyValueProjector<'AggregateId, 'Metadata, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: KeyValueProjectorConfig<'AggregateId, 'Metadata, 'Init, 'State, 'Events, 'KeyValueVersion>) = 
    member this.Project (item: EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events>): Async<unit> =
        async {
            try
                let aggregateId = item.AggregateId

                let snapshotAndVersion = 
                    aggregateId 
                    |> config.KeyValueStore.Get 
                    |> ResultModule.get

                let (newState, events, version) = 
                    match snapshotAndVersion, item.Data with
                    | None, EventLogItemData.Event event ->
                        sprintf "No state exists" |> ReadError.Error |> Result.Error
                    | Some (state, version), EventLogItemData.Init ->
                        sprintf "State already exists" |> ReadError.Error |> Result.Error
                    | None, EventLogItemData.Init init ->
                        init, 
                    | Some (state, version), EventLogItemData.Event event ->
                    |> ResultModule.get
        }
