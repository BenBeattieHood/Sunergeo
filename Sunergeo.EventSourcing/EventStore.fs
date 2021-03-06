﻿namespace Sunergeo.EventSourcing

open System
open Sunergeo.Core
open Sunergeo.EventSourcing.Storage


type EventStoreConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison> = {
    Fold: 'State -> 'Events -> 'State
    Logger: Sunergeo.Logging.Logger
    Create: 'AggregateId -> 'Init -> 'State
    Implementation: IEventStoreImplementation<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>
}

type EventStore<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion when 'AggregateId : comparison and 'KeyValueVersion : comparison>(config: EventStoreConfig<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>) = 
    
    let toEventLogItem
        (context: Context)
        (aggregateId: 'AggregateId)
        (data: EventLogItemData<_, _>)
        =
        {
            EventLogItem.Metadata = 
                {
                    EventLogItemMetadata.InstanceId = context.InstanceId
                    EventLogItemMetadata.AggregateId = aggregateId
                    EventLogItemMetadata.CorrelationId = Utils.createCorrelationId()
                    EventLogItemMetadata.FromCorrelationId = context.FromCorrelationId
                    EventLogItemMetadata.Timestamp = NodaTime.Instant()
                }
            EventLogItem.Data = data
        }

    member this.Create
        (context: Context) 
        (aggregateId: 'AggregateId) 
        (f: CreateCommandExec<'Init, 'Events>)
        : Async<Result<unit, Error>> =

        let apply
            (
                (init: 'Init),
                (newEvents: 'Events seq)
            )
            =
            let newEvents = 
                seq {
                    yield
                        init
                        |> EventLogItemData.Init

                    yield!
                        newEvents 
                        |> Seq.map EventLogItemData.Event
                }
                |> Seq.map (toEventLogItem context aggregateId)

            let newState =
                init
                |> config.Create aggregateId

            newState, newEvents, None

        config.Implementation.Append aggregateId
            (fun snapshotAndVersion ->
                match snapshotAndVersion with
                | Some snapshotAndVersion ->
                    (sprintf "Expected empty state, found %O" snapshotAndVersion)
                    |> Error.InvalidOp
                    |> Result.Error

                | None -> 
                    f context 
                    |> ResultModule.map apply
            )

    member this.Append
        (context: Context) 
        (aggregateId: 'AggregateId) 
        (f: UpdateCommandExec<'State, 'Events>)
        : Async<Result<unit, Error>> =

        let apply
            (snapshot: Snapshot<'State>)
            (newEvents: 'Events seq)
            (version: 'KeyValueVersion)
            =
            let newState = 
                newEvents
                |> Seq.fold config.Fold snapshot.State

            newState, (newEvents |> Seq.map EventLogItemData.Event |> Seq.map (toEventLogItem context aggregateId)), (version |> Some)

        config.Implementation.Append aggregateId
            (fun snapshotAndVersion ->
                match snapshotAndVersion with
                | None -> 
                    "State, found None"
                    |> Error.InvalidOp
                    |> Result.Error
                    
                | Some (snapshot, version) ->
                    f context snapshot.State 
                    |> ResultModule.map (fun newEvents -> apply snapshot newEvents version)
            )

    //member this.Delete(context: Context) (aggregateId: 'AggregateId) (f: DeleteCommandExec<'State>): Async<Result<unit, Error>> =
    //    let apply
    //        (snapshot: Snapshot<'State>)
    //        =
    //        let newEvents = seq {
    //            yield 
    //                context.Timestamp
    //                |> EventLogItem.End
    //        }

    //        newState, newEvents, None

    //    config.Implementation.Append aggregateId
    //        (fun snapshotAndVersion ->
    //            match snapshotAndVersion with
    //            | None -> 
    //                "State, found None"
    //                |> Error.InvalidOp
    //                |> Result.Error
                    
    //            | Some (snapshot, version) ->
    //                f context snapshot.State 
    //                |> ResultModule.map (fun _ -> apply snapshot)
    //        )