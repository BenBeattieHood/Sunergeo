namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.FSharp


module ProjectionUtils =
    let getShardPartitionsName 
        (shardPartitions: ShardPartition seq)
        : string =

        let stringJoin 
            (separator: string)
            (values: string seq)
            = String.Join(separator, values)

        shardPartitions 
        |> Seq.groupBy (fun shardPartition -> shardPartition.ShardId)
        |> Seq.map 
            (fun (shardId, shardPartition) -> 
                sprintf "[%s:%s]" shardId (shardPartition |> Seq.map (fun x -> x.ShardPartitionId |> string) |> stringJoin ",")
            )
        |> stringJoin "-"


type ProjectionHostConfig<'AggregateId, 'Metadata, 'Init, 'Events when 'AggregateId : comparison> = {
    Logger: Logger
    ShardPartitionListeners: (unit -> unit) list
    CommitEventSourceOffset: (ShardPartition * ShardPartitionOffset) -> unit
    CreateProjector: unit -> (EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events> -> unit)
}
type ProjectionHost<'AggregateId, 'Metadata, 'Init, 'Events when 'AggregateId : comparison>(config: ProjectionHostConfig<'AggregateId, 'Metadata, 'Init, 'Events>) as this = 
    
    let createProjectionActor
        (actorSystem: Akka.Actor.ActorSystem)
        (shardPartition: ShardPartition)
        : Akka.Actor.IActorRef =
        let projector = config.CreateProjector ()
        let projectionActorF =
            (fun (mailbox: Actor<EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events>>) ->
                let rec loop _ =
                    actor {
                        let! message = mailbox.Receive()
                        do message |> projector
                        return! loop ()
                    }
                loop ()
            )
        let actorId = sprintf "%s-%i-projector" shardPartition.ShardId shardPartition.ShardPartitionId
        let projectionActor = spawn actorSystem actorId projectionActorF
        projectionActor

    let mutable actors:Map<ShardPartition, Akka.Actor.IActorRef> = Map.empty
    let createOrLoadProjectionActorWith
        (createProjectionActor: ShardPartition -> Akka.Actor.IActorRef)
        (shardPartition: ShardPartition)
        : Akka.Actor.IActorRef =
        actors
        |> Map.tryFind shardPartition
        |> Option.defaultWith
            (fun _ -> 
                lock actors
                    (fun _ ->
                        actors
                        |> Map.tryFind shardPartition
                        |> Option.defaultWith
                            (fun _ ->
                                let projectionActor = createProjectionActor shardPartition
                                actors <- actors |> Map.add shardPartition projectionActor
                                projectionActor
                            )
                    )
            )
        
    let akkaConfigurationString = 
        Sunergeo.Akka.Configuration.ConfigurationBuilder.Create(
            defaultSerializer = typeof<Hyperion.Serializer>,
            byteArraySerializer = typeof<Hyperion.ValueSerializers.ByteArraySerializer>
            )

    let actorSystemName =
        config.ShardPartitionOffsets
        |> List.map fst
        |> ProjectionUtils.getShardPartitionsName
        
    let akkaConfiguration = Akka.Configuration.ConfigurationFactory.ParseString(akkaConfigurationString)
    let actorSystem = Akka.FSharp.System.create (actorSystemName + "-projectionhost") akkaConfiguration

    let createOrLoadProjectionActor =
        createOrLoadProjectionActorWith (createProjectionActor actorSystem)
    let shardPartitionListeningActors =
        config.ShardPartitionOffsets
        |> List.map
            (fun (shardPartition: ShardPartition, offset: ShardPartitionOffset) ->
                let shardPartitionListener = config.CreateShardPartitionListener (shardPartition, offset)
                let shardPartitionListeningActorF = 
                    (fun (mailbox: Actor<unit>) ->
                        let rec loop _ =
                            actor {
                                let! _ = mailbox.Receive()
                                let messages = shardPartitionListener ()

                                for message in messages do
                                    let projectionActor = createOrLoadProjectionActor message

                                return! loop ()
                            }
                        loop ()
                    )
                let actorId = sprintf "%s-%i-shardPartitionListeningActor" shardPartition.ShardId shardPartition.ShardPartitionId
                Spawn. actorSystem actorId shardPartitionListeningActorF
            )

    member this.OnEvent 
        (
            (aggregateId: 'AggregateId),
            (event: EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events>)
        ):unit =
        let actor = aggregateId |> createOrLoadProjectionActor actorSystem
        actor <! event
    
    interface System.IDisposable with
        member this.Dispose() = 
            actorSystem.Dispose()