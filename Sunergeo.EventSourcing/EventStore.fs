namespace Sunergeo.EventSourcing

open System
open Sunergeo.Core
open Sunergeo.EventSourcing.Storage


type EventStoreConfig<'PartitionId, 'State, 'Events when 'PartitionId : comparison> = {
    Create: Context -> 'PartitionId -> 'State
    Fold: 'State -> 'Events -> 'State
    Logger: Sunergeo.Logging.Logger
    Implementation: IEventStoreImplementation<'PartitionId, 'State, 'Events>
}

type EventStore<'PartitionId, 'State, 'Events when 'PartitionId : comparison>(config: EventStoreConfig<'PartitionId, 'State, 'Events>) = 
    
    member this.Create(context: Context) (partitionId: 'PartitionId) (f: CreateCommandExec<'Events>): Async<Result<unit, Error>> =
        let apply
            (newEvents: 'Events seq)
            =
            let newState = config.Create context partitionId

            let newEvents = seq {
                yield 
                    {
                        EventSourceInitItem.Id = partitionId
                        EventSourceInitItem.CreatedOn = context.Timestamp
                    }
                    |> EventLogItem.Init

                yield!
                    newEvents 
                    |> Seq.map EventLogItem.Event
            }

            newState, newEvents, None

        config.Implementation.Append partitionId
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

    member this.Append(context: Context) (partitionId: 'PartitionId) (f: UpdateCommandExec<'State, 'Events>): Async<Result<unit, Error>> =
        let apply
            (snapshot: Snapshot<'State>)
            (newEvents: 'Events seq)
            (version: int)
            =
            let newState = 
                newEvents
                |> Seq.fold config.Fold snapshot.State

            newState, (newEvents |> Seq.map EventLogItem.Event), (version |> Some)

        config.Implementation.Append partitionId
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