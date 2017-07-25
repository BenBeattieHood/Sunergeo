namespace Sunergeo.EventSourcing.Storage

open System
open System.Text
open Sunergeo.Core

type LogEntry<'Item> = {
    Position: int   // in Kafka this is called the offset, and it is available after the item has been written to a partition
    Item: 'Item
}

type Snapshot<'State> = {
    Position: int64
    State: 'State
}

type LogError =
    Timeout
    | Error of string

type EventStoreProcess<'PartitionId, 'State, 'Events when 'PartitionId : comparison> = 
    (Snapshot<'State> * int) option -> Result<'State * (EventLogItem<'PartitionId, 'Events> seq) * (int option), Error>

type IEventStoreImplementation<'PartitionId, 'State, 'Events, 'KeyValueVersion when 'PartitionId : comparison and 'KeyValueVersion : comparison> =
    
    abstract member Append:
        'PartitionId ->
        EventStoreProcess<'PartitionId, 'State, 'Events> ->
        Async<Result<unit, Error>>