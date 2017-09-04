// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open Sunergeo.Core
open Sunergeo.EventSourcing
open Sunergeo.EventSourcing.Kafka
open Sunergeo.EventSourcing.Storage
open Sunergeo.KeyValueStorage
open Sunergeo.KeyValueStorage.Memory
open Sunergeo.Logging
open Sunergeo.Projection
open Sunergeo.Projection.Kafka
open Sunergeo.Web
open Sunergeo.Web.Commands
open Sunergeo.Web.Queries
open Sunergeo.Examples.Turtle
open Sunergeo.Examples.Turtle.ReadStore
open Sunergeo.AkkaSignalr.Consumer

open ResultModule

open Sunergeo.Examples.Turtle.Core
open Sunergeo.Examples.Turtle.Events
open Sunergeo.Examples.Turtle.State
open Sunergeo.Examples.Turtle.Aggregate
open Sunergeo.Examples.Turtle.Commands
open Sunergeo.Examples.Turtle.Queries
open Microsoft.AspNetCore.Http
open System.IO
open Sunergeo.Kafka

// nb https://github.com/aspnet/KestrelHttpServer/issues/1652#issuecomment-293224372

let execCreateCommandFor<'AggregateId, 'Init, 'State, 'Events, 'Command, 'KeyValueVersion when 'Command :> ICreateCommand<'AggregateId, 'Init, 'Events> and 'AggregateId : comparison and 'KeyValueVersion : comparison>
    (eventStore: Sunergeo.EventSourcing.EventStore<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>)
    (command: 'Command)
    (context: Context)
    (request: HttpRequest)
    : Async<Result<unit, Error>> =
    eventStore.Create
        context
        (command.GetId context)
        command.Exec

let execCommandFor<'AggregateId, 'Init, 'State, 'Events, 'Command, 'KeyValueVersion when 'Command :> IUpdateCommand<'AggregateId, 'State, 'Events> and 'AggregateId : comparison and 'KeyValueVersion : comparison>
    (eventStore: Sunergeo.EventSourcing.EventStore<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>)
    (command: 'Command)
    (context: Context)
    (request: HttpRequest)
    : Async<Result<unit, Error>> =
    eventStore.Append
        context
        (command.GetId context)
        command.Exec

//let execCommandFor<'AggregateId, 'Init, 'State, 'Events, 'Command, 'KeyValueVersion when 'Command :> IDeleteCommand<'AggregateId, 'State> and 'AggregateId : comparison and 'KeyValueVersion : comparison>
//    (eventStore: Sunergeo.EventSourcing.EventStore<'AggregateId, 'Init, 'State, 'Events, 'KeyValueVersion>)
//    (command: 'Command)
//    (context: Context)
//    (request: HttpRequest)
//    : Async<Result<unit, Error>> =
//    eventStore.Delete
//        context
//        (command.GetId context)
//        command.Exec

let execQueryFor<'Query, 'ReadStore, 'Result when 'Query :> IQuery<'ReadStore, 'Result>>
    (readStore: 'ReadStore)
    (query: 'Query)
    (context: Context)
    (request: HttpRequest)
    : Async<Result<'Result, Error>> =
    query.Exec
        context
        readStore
        
        
//let ofExecQuery<'Result>
//    (execQuery: Context -> HttpRequest -> Async<Result<'Result option, Error>>)
//    : Context -> HttpRequest -> Async<Result<obj option, Error>> =
//    (fun (context: Context) (request: HttpRequest) ->
//        async {
//            let! result = execQuery context request
//            return result |> (ResultModule.map << Option.map) (fun x -> x :> obj)
//        }
//    )
let ofExecQuery<'Result>
    (execQuery: Context -> HttpRequest -> Async<Result<'Result, Error>>)
    : Context -> HttpRequest -> Async<Result<obj, Error>> =
    (fun (context: Context) (request: HttpRequest) ->
        async {
            let! result = execQuery context request
            return result |> ResultModule.map (fun x -> x :> obj)
        }
    )

[<EntryPoint>]
let main argv = 
    //let assemblies = [typeof<Turtle>.Assembly]

    let instanceId:InstanceId = 123
    let shardId =
        instanceId
        |> Utils.toShardId<Turtle>

    let eventStoreShardId:ShardId = shardId

    let logger =
        (fun (logLevel: LogLevel) (message: string) ->
            Console.WriteLine (sprintf "%O: %s" logLevel message)
        )

    sprintf "Starting server..."
    |> Console.WriteLine

    //let snapshotStoreConfig:KeyValueStoreConfig = 
    //    {
    //        Uri = Uri("localhost:3000")
    //        Logger = logger
    //        TableName = instanceId |> Utils.toShardId<Turtle>
    //    }

    let snapshotStore = new MemoryKeyValueStore<TurtleId, Snapshot<Turtle>>()

    //sprintf "Connected to snapshot store : %O" snapshotStoreConfig.Uri
    //|> Console.WriteLine
    
    let eventStoreSerializerConfig = 
        Hyperion.SerializerOptions(
            versionTolerance = false,
            preserveObjectReferences = false,
            surrogates = null,
            serializerFactories = null,
            knownTypes = null,
            ignoreISerializable = false
            )
    let eventStoreSerializer = Hyperion.Serializer(eventStoreSerializerConfig)
    let serializeEventStoreData
        (item: 'a)
        :byte[] =
        use stream = new MemoryStream()
        do eventStoreSerializer.Serialize(item, stream)
        stream.ToArray()

    let deserializeEventStoreData
        (bytes: byte[]) 
        :'a =
        use stream = new MemoryStream(bytes)
        let result = stream |> eventStoreSerializer.Deserialize
        result :?> 'a


        
    let eventStoreImplementationConfig:KafkaEventStoreImplementationConfig<TurtleId, TurtleInit, Turtle, TurtleEvent, MemoryKeyValueVersion> = 
        {
            ShardId = eventStoreShardId
            Logger = logger
            SnapshotStore = snapshotStore
            ProducerConfig = 
                {
                    KafkaProducerConfig.Default with
                        KafkaProducerConfig.BootstrapHosts =
                            [
                                { KafkaHost.Host = "localhost"; KafkaHost.Port = 9092 }
                            ]
                }
            SerializeAggregateId = serializeEventStoreData
            SerializeItem = serializeEventStoreData
        }
    use eventStoreImplementation = new KafkaEventStoreImplementation<TurtleId, TurtleInit, Turtle, TurtleEvent, MemoryKeyValueVersion>(eventStoreImplementationConfig)

    sprintf "Connected to kafka (%O)" eventStoreImplementationConfig.ProducerConfig.BootstrapHosts
    |> Console.WriteLine




    let eventSourceConfig:EventStoreConfig<TurtleId, TurtleInit, Turtle, TurtleEvent, MemoryKeyValueVersion> = 
        {
            Create = Turtle.create
            Fold = Turtle.fold
            Implementation = eventStoreImplementation
            Logger = logger
        }
    
    let eventStore = new Sunergeo.EventSourcing.EventStore<TurtleId, TurtleInit, Turtle, TurtleEvent, MemoryKeyValueVersion>(eventSourceConfig)

    let execCreateCommand = execCreateCommandFor eventStore
    let execCommand = execCommandFor eventStore

    sprintf "Initialized event store"
    |> Console.WriteLine


    
    let commandWebHostConfig:CommandWebHostConfig<TurtleId, State.Turtle, TurtleEvent> = 
        {
            InstanceId = instanceId
            Logger = logger
            BaseUri = Uri("http://localhost:8080")
            Handlers = 
                [
                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<CreateCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<CreateCommand>).Value.HttpMethod
                        RoutedCommand.Exec = (fun (x:CreateCommand) -> execCreateCommand x)
                    } |> Routing.createHandler
                    
                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<TurnLeftCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<TurnLeftCommand>).Value.HttpMethod
                        RoutedCommand.Exec = (fun (x:TurnLeftCommand) -> execCommand (x :> IUpdateCommand<TurtleId, Turtle, TurtleEvent>))
                    } |> Routing.createHandler

                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<TurnRightCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<TurnRightCommand>).Value.HttpMethod
                        RoutedCommand.Exec =  (fun (x:TurnRightCommand) -> execCommand (x :> IUpdateCommand<TurtleId, Turtle, TurtleEvent>))
                    } |> Routing.createHandler

                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<GoForwardsCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<GoForwardsCommand>).Value.HttpMethod
                        RoutedCommand.Exec =  (fun (x:GoForwardsCommand) -> execCommand (x :> IUpdateCommand<TurtleId, Turtle, TurtleEvent>))
                    } |> Routing.createHandler
                ]
        }

    use commandWebHost = 
        commandWebHostConfig
        |> CommandWebHost.create
    commandWebHost.Start()

    sprintf "Serving commands : %O" commandWebHostConfig.BaseUri
    |> Console.WriteLine

    
    //let readStoreConfig:KeyValueStoreConfig = 
    //    {
    //        Uri = Uri("localhost:3000")
    //        Logger = logger
    //        TableName = (instanceId |> Utils.toShardId<Turtle>) + "-ReadStore"
    //    }
    
    let shardPartitionPositionStore = MemoryKeyValueStore<ShardPartition, ShardPartitionPosition>()
    let readStore = MemoryKeyValueStore<TurtleId, Snapshot<DefaultReadStore.Turtle>>()



    let projectorConfig:Sunergeo.Projection.KeyValueStorage.KeyValueProjectorConfig<TurtleId, TurtleInit, DefaultReadStore.Turtle, TurtleEvent, MemoryKeyValueVersion> =
        {
            Logger = logger
            KeyValueStore = readStore
            CreateState = DefaultReadStore.create
            Fold = DefaultReadStore.fold
        }
    let projectionHostConfig:KafkaProjectionHostConfig<TurtleId, TurtleInit, TurtleEvent, MemoryKeyValueVersion> = 
        {
            ProjectionHostId = sprintf "%s-projectionhost" shardId
            Logger = logger
            ShardPartitionPositionStore = shardPartitionPositionStore
            Projectors =
                [
                    Sunergeo.Projection.KeyValueStorage.Implementation.keyValueProjector projectorConfig
                ]
            ProjectorsPerShardPartition = 5
            KafkaConsumerConfig =
                {
                    Sunergeo.Kafka.KafkaConsumerConfig.Default with
                        Sunergeo.Kafka.KafkaConsumerConfig.BootstrapHosts = eventStoreImplementationConfig.ProducerConfig.BootstrapHosts
                        Sunergeo.Kafka.KafkaConsumerConfig.ClientId = sprintf "%s-projectionhost" shardId   // assuming one projection host per box
                        Sunergeo.Kafka.KafkaConsumerConfig.GroupId = sprintf "%s-projectionhost" shardId
                }
            ShardId = eventStoreShardId
            Deserialize = deserializeEventStoreData
        }

    use projectionHost = new KafkaProjectionHost<TurtleId, TurtleInit, TurtleEvent, MemoryKeyValueVersion>(projectionHostConfig)
        
    sprintf "Initialized projection host and connected to kafka"
    |> Console.WriteLine


    let execQuery = execQueryFor (readStore :> IReadOnlyKeyValueStore<TurtleId, Snapshot<DefaultReadStore.Turtle>, MemoryKeyValueVersion>)
    
    let queryWebHostConfig:QueryWebHostConfig = 
        {
            InstanceId = instanceId
            Logger = logger
            Handlers = 
                [
                    ({
                        RoutedQuery.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<GetTurtle<_>>).Value.PathAndQuery
                        RoutedQuery.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<GetTurtle<_>>).Value.HttpMethod
                        RoutedQuery.Exec = 
                            (fun (x:GetTurtle<_>) -> 
                                execQuery (x :> IQuery<IReadOnlyKeyValueStore<TurtleId, Snapshot<DefaultReadStore.Turtle>, _>, DefaultReadStore.Turtle option>) |> ofExecQuery
                            )
                    } : RoutedQuery<GetTurtle<_>>)
                    |> Routing.createHandler<GetTurtle<_>, obj>
                ]
            BaseUri = Uri("http://localhost:8081")
        }

    use queryWebHost =
        queryWebHostConfig
        |> QueryWebHost.create
    queryWebHost.Start()

    sprintf "Serving queries : %O" queryWebHostConfig.BaseUri
    |> Console.WriteLine
    
    sprintf "Servers ready. Press enter to quit..."
    |> Console.WriteLine

    System.Console.ReadLine() |> ignore
    
    0 // return an integer exit code
