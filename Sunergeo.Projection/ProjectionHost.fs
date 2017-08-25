namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging
open Sunergeo.KeyValueStorage

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



type ProjectionActorMessage<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = 
    ShardPartitionPosition *
    EventLogItem<'AggregateId, 'Init, 'Events>

type Projector<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = 
    ShardPartition ->
        ShardPartitionPosition -> 
            EventLogItem<'AggregateId, 'Init, 'Events> -> 
                Async<Result<unit, Error>>

type ProjectionHostConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison> = {
    ProjectionHostId: string
    Logger: Logger
    ShardPartitionPositionStore: IKeyValueStore<ShardPartition, ShardPartitionPosition, 'ShardPartitionStoreKeyValueVersion>
    Projectors: Projector<'AggregateId, 'Init, 'Events> list
    ProjectorsPerShardPartition: int
}
type ProjectionHost<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison>(config: ProjectionHostConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion>) = 
    
    let createProjectionActor
        (shardPartitionPositionStore: IKeyValueStore<ShardPartition, ShardPartitionPosition, 'ShardPartitionStoreKeyValueVersion>)
        (actorSystem: Akka.Actor.ActorSystem)
        (shardPartition: ShardPartition)
        (projectionPartitionIndex: int)
        (projectionActorIndex: int)
        (projector: Projector<'AggregateId, 'Init, 'Events>)
        : Akka.Actor.IActorRef =
        let projectionActorF =
            (fun (mailbox: Actor<ProjectionActorMessage<'AggregateId, 'Init, 'Events>>) ->
                let rec loop _ =
                    actor {
                        let! position, message = mailbox.Receive()
                        do projector shardPartition position message
                            |> Async.RunSynchronously
                            |> ResultModule.get
                        
                        do position 
                            |> KeyValueStorageUtils.createOrPut shardPartitionPositionStore shardPartition
                            |> ResultModule.get

                        return! loop ()
                    }
                loop ()
            )
        let actorId = sprintf "%s-%i-%i-%i-projector" shardPartition.ShardId shardPartition.ShardPartitionId projectionPartitionIndex projectionActorIndex
        let projectionActor = spawn actorSystem actorId projectionActorF
        projectionActor

    let createProjectionPartition
        (shardPartitionPositionStore: IKeyValueStore<ShardPartition, ShardPartitionPosition, 'ShardPartitionStoreKeyValueVersion>)
        (actorSystem: Akka.Actor.ActorSystem)
        (shardPartition: ShardPartition)
        (projectionPartitionIndex: int)
        (projectors: Projector<'AggregateId, 'Init, 'Events> list)
        : Akka.Actor.IActorRef list =

        projectors
        |> List.mapi (createProjectionActor shardPartitionPositionStore actorSystem shardPartition projectionPartitionIndex)

    let getPartitionForId
        (id: 'a)
        (targets: 'b list)
        : 'b =
        let index = ((id.GetHashCode() / 3) + 1073741823) % targets.Length // deterministic-within-runtime positive modulo - assumes projection actors die when runtime dies
        targets.[index]

    let mutable actors:Map<ShardPartition, Akka.Actor.IActorRef list list> = Map.empty

    let createOrLoadProjectionPartitionWith
        (projectors: Projector<'AggregateId, 'Init, 'Events> list)
        (projectorsPerShardPartition: int)
        (shardPartitionPositionStore: IKeyValueStore<ShardPartition, ShardPartitionPosition, 'ShardPartitionStoreKeyValueVersion>)
        (actorSystem: Akka.Actor.ActorSystem)
        (shardPartition: ShardPartition)
        (aggregateId: 'AggregateId)
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
                                                createProjectionPartition shardPartitionPositionStore actorSystem shardPartition projectionPartitionIndex projectors
                                            )
                                    actors <- actors |> Map.add shardPartition projectionPartitions

                                    projectionPartitions
                                )
                        )
                )
        projectionPartitions |> getPartitionForId aggregateId
        
    let akkaConfigurationString = 
        Sunergeo.Akka.Configuration.ConfigurationBuilder.Create(
            defaultSerializer = typeof<Akka.Serialization.HyperionSerializer>,
            byteArraySerializer = typeof<Akka.Serialization.ByteArraySerializer>
            )

    let akkaConfiguration = Akka.Configuration.ConfigurationFactory.ParseString(akkaConfigurationString)
    let actorSystem = Akka.FSharp.System.create (config.ProjectionHostId + "-projectionhost") akkaConfiguration
    
    let createOrLoadProjectionPartition =
        createOrLoadProjectionPartitionWith
            config.Projectors
            config.ProjectorsPerShardPartition
            config.ShardPartitionPositionStore
            actorSystem

    member this.OnEvent
        (shardPartition: ShardPartition)
        (shardPartitionPosition: ShardPartitionPosition)
        (item: EventLogItem<'AggregateId, 'Init, 'Events>)
        :unit =
        let projectionPartition = createOrLoadProjectionPartition shardPartition item.Metadata.AggregateId
        for projectionActor in projectionPartition do
            let projectionActorMessage:ProjectionActorMessage<'AggregateId, 'Init, 'Events> =
                shardPartitionPosition,
                item
            projectionActor <! projectionActorMessage

    //member this.OnEvent 
    //    (
    //        (aggregateId: 'AggregateId),
    //        (event: EventLogItem<'AggregateId, 'Init, 'Events>)
    //    ):unit =
    //    let actor = aggregateId |> createOrLoadProjectionPartition actorSystem
    //    actor <! event

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

    
    interface System.IDisposable with
        member this.Dispose() = 
            actorSystem.Dispose()