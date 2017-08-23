// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open Sunergeo.Core
open Sunergeo.EventSourcing
open Sunergeo.EventSourcing.Memory
open Sunergeo.EventSourcing.Storage
open Sunergeo.KeyValueStorage
open Sunergeo.KeyValueStorage.Memory
open Sunergeo.Logging
open Sunergeo.Projection
open Sunergeo.Projection.Default
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

// nb https://github.com/aspnet/KestrelHttpServer/issues/1652#issuecomment-293224372

let execCreateCommandFor<'AggregateId, 'Init, 'State, 'Events, 'Command, 'KeyValueVersion when 'Command :> ICreateCommand<'AggregateId, 'State, 'Events> and 'AggregateId : comparison and 'KeyValueVersion : comparison>
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

    let instanceId:InstanceId = "123"

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

    let eventStoreImplementationConfig:MemoryEventStoreImplementationConfig<TurtleId, Turtle, MemoryKeyValueVersion> = 
        {
            InstanceId = instanceId
            Logger = logger
            SnapshotStore = snapshotStore
        }
    let eventStoreImplementation = MemoryEventStoreImplementation<TurtleId, Turtle, Turtle, TurtleEvent, MemoryKeyValueVersion>(eventStoreImplementationConfig)

    let eventSourceConfig:EventStoreConfig<TurtleId, Turtle, Turtle, TurtleEvent, MemoryKeyValueVersion> = 
        {
            CreateInit = (fun _ _ state -> state)
            Fold = Turtle.fold
            Implementation = eventStoreImplementation
            Logger = logger
        }
    
    let eventStore = new Sunergeo.EventSourcing.EventStore<TurtleId, Turtle, Turtle, TurtleEvent, MemoryKeyValueVersion>(eventSourceConfig)

    let execCreateCommand = execCreateCommandFor eventStore
    let execCommand = execCommandFor eventStore

    //sprintf "Connected to kafka : %O" eventSourceConfig.LogUri
    //|> Console.WriteLine
    
    let commandWebHostConfig:CommandWebHostConfig<TurtleId, State.Turtle, TurtleEvent> = 
        {
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

    let readStore = MemoryKeyValueStore()

    // use akkaSignalr = Sunergeo.AkkaSignalr.Consumer.Program.StartAkkaAndSignalr()
    
    let mutable pollPositionState:Map<TurtleId, int> = Map.empty
    let pollingActorConfig:Sunergeo.Projection.EventSourcePollingActorConfig<TurtleId, Turtle, TurtleEvent, MemoryKeyValueVersion> = 
        {
            Logger = logger
            EventSource = eventStoreImplementation :> Sunergeo.EventSourcing.Memory.IEventSource<TurtleId, Turtle, TurtleEvent>
            GetPollPositionState =
                (fun _ ->
                    async { return pollPositionState }
                )
            SetPollPositionState = 
                (fun x ->
                    async { pollPositionState <- x }
                )
        }
        //KafkaPollingActorConfig = 
        //    {
        //        KafkaUri = eventSourceConfig.LogUri
        //        GroupId = instanceId |> Utils.toShardId<Turtle>
        //        AutoCommitIntervalMs = 5000 |> Some
        //        StatisticsIntervalMs = 60000
        //        Servers = "localhost:9092"
        //    }
        
    let projectionHostConfig:ProjectionHostConfig<KeyValueStorageProjectionConfig<TurtleId, Turtle, DefaultReadStore.Turtle, TurtleEvent, MemoryKeyValueVersion>, TurtleId, Turtle, TurtleEvent, EventSourcePollingActor<TurtleId, Turtle, DefaultReadStore.Turtle, TurtleEvent, MemoryKeyValueVersion>> = 
        {
            Logger = logger
            InstanceId = instanceId
            ActorConfig = 
                {
                    Logger = logger
                    KeyValueStore = readStore
                    CreateState = DefaultReadStore.create
                    FoldState = DefaultReadStore.fold
                }
            CreatePollingActor = 
                (fun instanceId onEvent ->
                    EventSourcePollingActor(pollingActorConfig, instanceId, onEvent)
                )
        }

    use projectionHost = new KeyValueStoreProjectorHost<TurtleId, Turtle, DefaultReadStore.Turtle, TurtleEvent, MemoryKeyValueVersion, EventSourcePollingActor<TurtleId, Turtle, DefaultReadStore.Turtle, TurtleEvent, MemoryKeyValueVersion>>(projectionHostConfig)
        
    let execQuery = execQueryFor (readStore :> IReadOnlyKeyValueStore<TurtleId, DefaultReadStore.Turtle, _>)
    
    
//type RoutedType<'HandlerType, 'Result> = {
//    PathAndQuery: string
//    HttpMethod: HttpMethod
//    Exec: 'HandlerType -> Context -> HttpRequest -> Async<Result<'Result, Error>>
//}

//type RoutedTypeRequestHandler<'Result> = 
//    Context -> Microsoft.AspNetCore.Http.HttpRequest -> Option<unit -> Async<Result<'Result, Error>>>


    let queryWebHostConfig:QueryWebHostConfig = 
        {
            Logger = logger
            Handlers = 
                [
                    ({
                        RoutedQuery.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<GetTurtle<_>>).Value.PathAndQuery
                        RoutedQuery.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<GetTurtle<_>>).Value.HttpMethod
                        RoutedQuery.Exec = 
                            (fun (x:GetTurtle<_>) -> 
                                execQuery (x :> IQuery<IReadOnlyKeyValueStore<TurtleId, DefaultReadStore.Turtle, _>, DefaultReadStore.Turtle option>) |> ofExecQuery
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
