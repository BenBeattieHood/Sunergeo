﻿namespace Sunergeo.EventSourcing.Storage

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

type EventStoreProcess<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = 
    (Snapshot<'State> * 'KeyValueVersion) option -> Result<'State * (EventLogItem<'AggregateId, 'Init, 'Events> seq) * ('KeyValueVersion option), Error>

type IEventStoreImplementation<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> =
    
    abstract member Append:
        'AggregateId ->
        EventStoreProcess<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion> ->
        Async<Result<unit, Error>>