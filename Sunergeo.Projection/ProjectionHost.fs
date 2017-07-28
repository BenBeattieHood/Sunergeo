namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor


type ProjectionHostConfig<'ActorConfig, 'AggregateId, 'Init, 'Events, 'PollingActor when 'AggregateId : comparison> = {
    Logger: Logger
    InstanceId: InstanceId
    ActorConfig: 'ActorConfig
    CreatePollingActor: InstanceId -> (('AggregateId * EventLogItem<'AggregateId, 'Init, 'Events>) -> unit) -> 'PollingActor
}

[<AbstractClass>]
type ProjectionHost<'ActorConfig, 'AggregateId, 'Init, 'State, 'Events, 'PollingActor when 'AggregateId : comparison>(config: ProjectionHostConfig<'ActorConfig, 'AggregateId, 'Init, 'Events, 'PollingActor>) as this = 
    let shardId = 
        config.InstanceId 
        |> Utils.toShardId<'State>

    let actorSystem = ActorSystem.Create shardId

    let mutable actors:Map<'AggregateId, Akka.Actor.IActorRef> = Map.empty
    let createOrLoadActor 
        (aggregateId: 'AggregateId)
        : Akka.Actor.IActorRef =
        lock actors
            (fun _ ->
                actors
                |> Map.tryFind aggregateId
                |> Option.defaultWith
                    (fun _ ->
                        let projectionActorProps = Akka.Actor.Props.Create(System.Func<unit, Projector<'AggregateId, 'Init, 'Events>>(fun _ -> this.CreateActor config.ActorConfig aggregateId))
                        let actor = actorSystem.ActorOf(projectionActorProps)
                        actors <- actors |> Map.add aggregateId actor
                        actor
                    )
            )

    let onEvent
        (
            (aggregateId: 'AggregateId),
            (event: EventLogItem<'AggregateId, 'Init, 'Events>)
        ):unit =
        let actor = aggregateId |> createOrLoadActor
        actor.Tell(event)
            
    let pollingActorProps = Akka.Actor.Props.Create(System.Func<unit, 'PollingActor>(fun _ -> config.CreatePollingActor config.InstanceId onEvent))
    let pollingActor = actorSystem.ActorOf(pollingActorProps)
    
    abstract member CreateActor: 'ActorConfig -> 'AggregateId -> Projector<'AggregateId, 'Init, 'Events>
    
    interface System.IDisposable with
        member this.Dispose() = 
            actorSystem.Dispose()