namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor

type ProjectionHostConfig<'ActorConfig, 'PartitionId> = {
    Logger: Logger
    InstanceId: InstanceId
    KafkaUri: Uri
    ActorConfig: 'ActorConfig
    KafkaPollingActorConfig: KafkaPollingActorConfig
    GetPartitionId: int -> 'PartitionId
}

//type ProjectionHost<'PartitionId, 'Events, 'State when 'PartitionId : comparison>(config: ProjectionHostConfig) = 
[<AbstractClass>]
type ProjectionHost<'ActorConfig, 'PartitionId, 'State, 'Events when 'PartitionId : comparison>(config: ProjectionHostConfig<'ActorConfig, 'PartitionId>) as this = 
    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>

    let kafkaConsumerGroupName = topic + "-group"

    let actorSystem = ActorSystem.Create topic

    let mutable actors:Map<'PartitionId, Akka.Actor.IActorRef> = Map.empty
    let createOrLoadActor 
        (partitionId: 'PartitionId)
        : Akka.Actor.IActorRef =
        lock actors
            (fun _ ->
                actors
                |> Map.tryFind partitionId
                |> Option.defaultWith
                    (fun _ ->
                        let projectionActorProps = Akka.Actor.Props.Create(fun _ -> this.CreateActor config.ActorConfig partitionId)
                        let actor = actorSystem.ActorOf(projectionActorProps)
                        actors <- actors |> Map.add partitionId actor
                        actor
                    )
            )

    let onKafkaMessage
        (pollingActorRef: IActorRef)
        (message: Confluent.Kafka.Message)
        :unit =
        if message.Topic = topic
        then
            let partitionId = message.Partition |> config.GetPartitionId
            let actor = partitionId |> createOrLoadActor
            let events:EventLogItem<'PartitionId, 'Events> = Sunergeo.Core.Todo.todo()
            actor.Tell(events, pollingActorRef)
        else
            sprintf "Received message for unexpected topic %s (listening on %s)" message.Topic topic
            |> config.Logger LogLevel.Error

    let pollingActorProps = Akka.Actor.Props.Create(fun _ -> KafkaPollingActor(config.KafkaPollingActorConfig, onKafkaMessage))
    let pollingActor = actorSystem.ActorOf(pollingActorProps)
    
    abstract member CreateActor: 'ActorConfig -> 'PartitionId -> Projector<'PartitionId, 'Events>
