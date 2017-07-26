namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor


type ProjectionHostConfig<'ActorConfig, 'ProjectionId, 'Init, 'Events, 'PollingActor when 'ProjectionId : comparison> = {
    Logger: Logger
    InstanceId: InstanceId
    ActorConfig: 'ActorConfig
    CreatePollingActor: InstanceId -> (('ProjectionId * EventLogItem<'ProjectionId, 'Init, 'Events>) -> unit) -> 'PollingActor
}

[<AbstractClass>]
type ProjectionHost<'ActorConfig, 'ProjectionId, 'Init, 'State, 'Events, 'PollingActor when 'ProjectionId : comparison>(config: ProjectionHostConfig<'ActorConfig, 'ProjectionId, 'Init, 'Events, 'PollingActor>) as this = 
    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>

    let actorSystem = ActorSystem.Create topic

    let mutable actors:Map<'ProjectionId, Akka.Actor.IActorRef> = Map.empty
    let createOrLoadActor 
        (projectionId: 'ProjectionId)
        : Akka.Actor.IActorRef =
        lock actors
            (fun _ ->
                actors
                |> Map.tryFind projectionId
                |> Option.defaultWith
                    (fun _ ->
                        let projectionActorProps = Akka.Actor.Props.Create(System.Func<unit, Projector<'ProjectionId, 'Init, 'Events>>(fun _ -> this.CreateActor config.ActorConfig projectionId))
                        let actor = actorSystem.ActorOf(projectionActorProps)
                        actors <- actors |> Map.add projectionId actor
                        actor
                    )
            )

    let onEvent
        (
            (projectionId: 'ProjectionId),
            (event: EventLogItem<'ProjectionId, 'Init, 'Events>)
        ):unit =
        let actor = projectionId |> createOrLoadActor
        actor.Tell(event)
            
    let pollingActorProps = Akka.Actor.Props.Create(System.Func<unit, 'PollingActor>(fun _ -> config.CreatePollingActor config.InstanceId onEvent))
    let pollingActor = actorSystem.ActorOf(pollingActorProps)
    
    abstract member CreateActor: 'ActorConfig -> 'ProjectionId -> Projector<'ProjectionId, 'Init, 'Events>
    
    interface System.IDisposable with
        member this.Dispose() = 
            actorSystem.Dispose()