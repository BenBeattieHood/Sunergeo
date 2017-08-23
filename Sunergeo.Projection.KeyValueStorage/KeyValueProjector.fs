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
type KeyValueProjector<'AggregateId, 'Metadata, 'State, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: KeyValueProjectorConfig<'AggregateId, 'Metadata, 'State, 'KeyValueVersion>) = 
    member this.Project (aggregateId: 'AggregateId, item: EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events>): unit =
        config.KeyValueStore.Get aggregateId
