namespace Sunergeo.Projection

open Sunergeo.Core

open System
open Akka.Actor

type ProjectionHostConfig<'ActorConfig> = {
    InstanceId: InstanceId
    KafkaUri: Uri
    ActorConfig: 'ActorConfig
    KafkaPollingActorConfig: KafkaPollingActorConfig
}

//type ProjectionHost<'PartitionId, 'Events, 'State when 'PartitionId : comparison>(config: ProjectionHostConfig) = 
[<AbstractClass>]
type ProjectionHost<'ActorConfig, 'PartitionId, 'Events when 'PartitionId : comparison>(config: ProjectionHostConfig<'ActorConfig>) as this = 
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
        (message: Confluent.Kafka.Message)
        :unit =
        ()

    let pollingActorProps = Akka.Actor.Props.Create(fun _ -> KafkaPollingActor(config.KafkaPollingActorConfig, onKafkaMessage))
    let pollingActor = actorSystem.ActorOf(pollingActorProps)
    
    abstract member CreateActor: 'ActorConfig -> 'PartitionId -> Projector<'PartitionId, 'Events>
