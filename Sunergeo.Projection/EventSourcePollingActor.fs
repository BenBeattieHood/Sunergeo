namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor

type EventSourcePollingActorConfig<'AggregateId, 'Init, 'Events, 'StateKeyValueVersion when 'AggregateId : comparison and 'StateKeyValueVersion : comparison> = {
    Logger: Logger
    EventSource: Sunergeo.EventSourcing.Memory.IEventSource<'AggregateId, 'Init, 'Events>
    GetPollPositionState: unit -> Async<Map<'AggregateId, int>>
    SetPollPositionState: Map<'AggregateId, int> -> Async<unit>
}
type EventSourcePollingActor<'AggregateId, 'Init, 'State, 'Events, 'StateKeyValueVersion when 'AggregateId : comparison and 'StateKeyValueVersion : comparison>(config: EventSourcePollingActorConfig<'AggregateId, 'Init, 'Events, 'StateKeyValueVersion>, instanceId: InstanceId, onEvent: ('AggregateId * EventLogItem<'AggregateId, 'Init, 'Events>) -> unit) as this =
    inherit ReceiveActor()

    let tryFinally (body : Async<'T>) (finallyF : Async<unit>) = 
        async {
            let! ct = Async.CancellationToken
            return! Async.FromContinuations(fun (sc,ec,cc) ->
                let sc' (t : 'T) = Async.StartWithContinuations(finallyF, (fun () -> sc t), ec, cc, ct)
                let ec' (e : exn) = Async.StartWithContinuations(finallyF, (fun () -> ec e), ec, cc, ct)
                Async.StartWithContinuations(body, sc', ec', cc, ct))
        }
    
    let getNewData () = 
        async {
            let! pollState = config.GetPollPositionState()

            let! positions = 
                config.EventSource.GetPositions()

            let! results =
                positions
                |> Map.toSeq
                |> Seq.choose
                    (fun (aggregateId, positionId) ->
                        match pollState |> Map.tryFind aggregateId with
                        | Some processedPositionId when processedPositionId = positionId -> 
                            None
                        | Some processedPositionId ->
                            Some (aggregateId, processedPositionId)
                        | None ->
                            Some (aggregateId, 0)
                    )
                |> Seq.map
                    (fun (aggregateId, positionId) ->
                        async {
                            let! result = config.EventSource.ReadFrom aggregateId positionId
                            return aggregateId, result
                        }
                    )
                |> Async.Parallel

            let results = results |> Array.choose (fun (aggregateId, logEntriesOption) -> logEntriesOption |> Option.map (fun logEntries -> aggregateId, logEntries))
                
            let mutable pollState = pollState

            for (aggregateId, logEntries) in results do
                for logEntry in logEntries do
                    onEvent (aggregateId, logEntry.Item)

                    pollState <-
                        pollState
                        |> Map.add aggregateId logEntry.Position

                    do! config.SetPollPositionState pollState
        }
        
    let shardId = 
        instanceId 
        |> Utils.toShardId<'State>
            
    let self = this.Self
    do this.Receive<unit>
        (fun _ -> 
            getNewData() |> Async.RunSynchronously
            self.Tell(())
        )

    override this.PreStart() =
        ReceiveActor.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100.0), self, (), ActorRefs.Nobody)
        base.PreStart()
    