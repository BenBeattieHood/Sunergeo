namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.FSharp
open Akka.Util


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




type Projector<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = 
    ShardPartition ->
        ShardPartitionPosition -> 
            EventLogItem<'AggregateId, 'Init, 'Events> -> 
                Async<Result<unit, Error>>

type ProjectionHostConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison> = {
    Logger: Logger
    ShardPartitionStore: Sunergeo.KeyValueStorage.IKeyValueStore<ShardPartition, ShardPartitionPosition, 'ShardPartitionStoreKeyValueVersion>
    Projectors: Projector<'AggregateId, 'Init, 'Events> list
    ProjectorsPerShardPartition: int
}
type ProjectionHost<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison>(config: ProjectionHostConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion>) as this = 
    
    let createProjectionActor
        (actorSystem: Akka.Actor.ActorSystem)
        (shardPartition: ShardPartition)
        (projectionPartitionIndex: int)
        (projectionActorIndex: int)
        (projector: Projector<'AggregateId, 'Init, 'Events>)
        : Akka.Actor.IActorRef =
        let projectionActorF =
            (fun (mailbox: Actor<ShardPartitionPosition * EventLogItem<'AggregateId, 'Init, 'Events>>) ->
                let rec loop _ =
                    actor {
                        let! position, message = mailbox.Receive()
                        do projector shardPartition position message
                            |> Async.RunSynchronously
                            |> ResultModule.get
                        return! loop ()
                    }
                loop ()
            )
        let actorId = sprintf "%s-%i-%i-%i-projector" shardPartition.ShardId shardPartition.ShardPartitionId projectionPartitionIndex projectionActorIndex
        let projectionActor = spawn actorSystem actorId projectionActorF
        projectionActor

    let createProjectionPartition
        (actorSystem: Akka.Actor.ActorSystem)
        (shardPartition: ShardPartition)
        (projectionPartitionIndex: int)
        (projectors: Projector<'AggregateId, 'Init, 'Events> list)
        : Akka.Actor.IActorRef list =

        projectors
        |> List.mapi (createProjectionActor actorSystem shardPartition projectionPartitionIndex)

    let getPartitionForId
        (id: 'a)
        (targets: 'b list)
        : 'b =
        let index = ((id.GetHashCode() / 3) + 1073741823) % targets.Length // deterministic-within-runtime positive modulo - assumes projection actors die when runtime dies
        targets.[index]

    let mutable actors:Map<ShardPartition, Akka.Actor.IActorRef list list> = Map.empty

    let createOrLoadProjectionPartitionWith
        (projectors: Projector<'AggregateId, 'Init, 'Events> list)
        (actorSystem: Akka.Actor.ActorSystem)
        (shardPartition: ShardPartition)
        (aggregateId: 'AggregateId)
        (projectorsPerShardPartition: int)
        : Akka.Actor.IActorRef list =
        let projectionPartitions =
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

                                    let projectionPartitions = 
                                        [0 .. projectorsPerShardPartition - 1]
                                        |> List.map
                                            (fun projectionPartitionIndex -> 
                                                createProjectionPartition actorSystem shardPartition projectionPartitionIndex projectors
                                            )
                                    actors <- actors |> Map.add shardPartition projectionPartitions

                                    projectionPartitions
                                )
                        )
                )
        projectionPartitions |> getPartitionForId aggregateId
        
//    let akkaConfigurationString = 
//        Sunergeo.Akka.Configuration.ConfigurationBuilder.Create(
//            defaultSerializer = typeof<Hyperion.Serializer>,
//            byteArraySerializer = typeof<Hyperion.ValueSerializers.ByteArraySerializer>
//            )

//    let actorSystemName =
//        config.ShardPartitionPositions
//        |> List.map fst
//        |> ProjectionUtils.getShardPartitionsName
        
//    let akkaConfiguration = Akka.Configuration.ConfigurationFactory.ParseString(akkaConfigurationString)
//    let actorSystem = Akka.FSharp.System.create (actorSystemName + "-projectionhost") akkaConfiguration

//    let createOrLoadProjectionActor =
//        createOrLoadProjectionActorWith (createProjectionActor actorSystem)
//    let shardPartitionListeningActors =
//        config.ShardPartitionPositions
//        |> List.map
//            (fun (shardPartition: ShardPartition, shardPartitionPosition: ShardPartitionPosition) ->
//                let shardPartitionListener = config.CreateShardPartitionListener (shardPartition, shardPartitionPosition)
//                let shardPartitionListeningActorF = 
//                    (fun (mailbox: Actor<unit>) ->
//                        let rec loop _ =
//                            actor {
//                                let! _ = mailbox.Receive()
//                                let messages = shardPartitionListener ()

//                                for message in messages do
//                                    let projectionActor = createOrLoadProjectionActor message

//                                return! loop ()
//                            }
//                        loop ()
//                    )
//                let actorId = sprintf "%s-%i-shardPartitionListeningActor" shardPartition.ShardId shardPartition.ShardPartitionId
//                Spawn. actorSystem actorId shardPartitionListeningActorF
//            )

//    member this.OnEvent 
//        (
//            (aggregateId: 'AggregateId),
//            (event: EventLogItem<'AggregateId, 'Init, 'Events>)
//        ):unit =
//        let actor = aggregateId |> createOrLoadProjectionActor actorSystem
//        actor <! event
    
//    interface System.IDisposable with
//        member this.Dispose() = 
//            actorSystem.Dispose()