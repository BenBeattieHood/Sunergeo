// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open Sunergeo.Core
open Sunergeo.EventSourcing
open Sunergeo.KeyValueStorage
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
open Microsoft.AspNetCore.Http

let execCreateCommandFor<'PartitionId, 'State, 'Events, 'Command when 'Command :> ICreateCommand<'PartitionId, 'State, 'Events> and 'PartitionId : comparison>
    (eventSource: Sunergeo.EventSourcing.EventSource<'PartitionId, 'State, 'Events>)
    (command: 'Command)
    (context: Context)
    (request: HttpRequest)
    : Async<Result<unit, Error>> =
    eventSource.Create context (command.GetId context) command.Exec

let execCommandFor<'PartitionId, 'State, 'Events, 'Command when 'Command :> IUpdateCommand<'PartitionId, 'State, 'Events> and 'PartitionId : comparison>
    (eventSource: Sunergeo.EventSourcing.EventSource<'PartitionId, 'State, 'Events>)
    (command: IUpdateCommand<'PartitionId, 'State, 'Events>)
    (context: Context)
    (request: HttpRequest)
    : Async<Result<unit, Error>> =
    eventSource.Append context (command.GetId context) command.Exec

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

    let snapshotStoreConfig:KeyValueStorageConfig = 
        {
            Uri = Uri("localhost:3000")
            Logger = logger
            TableName = instanceId |> Utils.toTopic<Turtle>
        }

    use snapshotStore = new KeyValueStore<TurtleId, Snapshot<Turtle>>(snapshotStoreConfig)

    sprintf "Connected to snapshot store : %O" snapshotStoreConfig.Uri
    |> Console.WriteLine

    let eventSourceConfig:EventSourceConfig<TurtleId, Turtle, TurtleEvent> = 
        {
            InstanceId = instanceId
            Create = Turtle.create
            Fold = Turtle.fold
            SnapshotStore = snapshotStore
            LogUri = Uri("localhost:9092")
            Logger = logger
        }
    
    use eventSource = new Sunergeo.EventSourcing.EventSource<TurtleId, Turtle, TurtleEvent>(eventSourceConfig)

    let execCreateCommand = execCreateCommandFor eventSource
    let execCommand = execCommandFor eventSource

    sprintf "Connected to kafka : %O" eventSourceConfig.LogUri
    |> Console.WriteLine
    
    let commandWebHostConfig:CommandWebHostConfig<TurtleId, State.Turtle, TurtleEvent> = 
        {
            Logger = logger
            BaseUri = Uri("http://localhost:8080")
            Handlers = 
                [
                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<CreateCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<CreateCommand>).Value.HttpMethod
                        RoutedCommand.Exec = 
                            (fun command context request ->
                                eventSource.Create
                                    context
                                    ((command :> ICreateCommand<TurtleId, Turtle, TurtleEvent>).GetId())
                                    ((command :> ICreateCommand<TurtleId, Turtle, TurtleEvent>).Exec context)
                            )
                    } |> CommandWebHost.toGeneralRoutedCommand |> Routing.createHandler
                    
                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<TurnLeftCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<TurnLeftCommand>).Value.HttpMethod
                        RoutedCommand.Exec = 
                            (fun command context request ->
                                eventSource.Append
                                    context
                                    ((command :> IUpdateCommand<TurtleId, Turtle, TurtleEvent>).GetId())
                                    ((command :> IUpdateCommand<TurtleId, Turtle, TurtleEvent>).Exec context)
                            )
                    } |> CommandWebHost.toGeneralRoutedCommand |> Routing.createHandler

                    //{
                    //    RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<TurnRightCommand>).Value.PathAndQuery
                    //    RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<TurnRightCommand>).Value.HttpMethod
                    //    RoutedCommand.Exec = 
                    //        (fun (command: TurnRightCommand) (context: Context) ->
                    //            Sunergeo.Core.Todo.todo()
                    //            //(command :> ICreateCommand).Exec context
                    //        )
                    //} |> Routing.createHandler

                    //{
                    //    RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<MovedForwardsCommand>).Value.PathAndQuery
                    //    RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<MovedForwardsCommand>).Value.HttpMethod
                    //    RoutedCommand.Exec = 
                    //        (fun (command: MovedForwardsCommand) (context: Context) ->
                    //            Sunergeo.Core.Todo.todo()
                    //            //(command :> ICreateCommand).Exec context
                    //        )
                    //} |> Routing.createHandler
                ]
        }

    use commandWebHost = 
        commandWebHostConfig
        |> CommandWebHost.create
    commandWebHost.Start()

    sprintf "Serving commands : %O" commandWebHostConfig.BaseUri
    |> Console.WriteLine

    
    let readStoreConfig:KeyValueStorageConfig = 
        {
            Uri = Uri("localhost:3000")
            Logger = logger
            TableName = (instanceId |> Utils.toTopic<Turtle>) + "-ReadStore"
        }   

    use akkaSignalr = Sunergeo.AkkaSignalr.Consumer.Program.StartAkkaAndSignalr()
        
    let kafkaProjectionHostConfig:ProjectionHostConfig<KeyValueStorageProjectionConfig<TurtleId, DefaultReadStore.Turtle, TurtleEvent>, TurtleId> = {
        Logger = logger
        InstanceId = instanceId
        KafkaUri = eventSourceConfig.LogUri
        ActorConfig = 
            {
                Logger = logger
                KeyValueStorageConfig = readStoreConfig
                CreateState = DefaultReadStore.create
                FoldState = DefaultReadStore.fold
            }
        KafkaPollingActorConfig = 
            {
                GroupId = "tuneup"
                AutoCommitIntervalMs = 5000 |> Some
                StatisticsIntervalMs = 60000
                Servers = "localhost:9092"
            }
        GetPartitionId = string
    }

    use kafkaProjectionHost = new KeyValueStoreProjectorHost<TurtleId, DefaultReadStore.Turtle, TurtleEvent>(kafkaProjectionHostConfig)
        

    let queryWebHostConfig:QueryWebHostConfig = 
        {
            Logger = logger
            Queries = 
                [
                ]
                |> List.map QueryWebHost.toGeneralRoutedQuery
            BaseUri = Uri("http://localhost:8081")
            ContextProvider = 
                (fun (httpContext:HttpContext) -> 
                    Console.WriteLine("Called Context Provider...")
                    {
                        // TODO:
                        Context.UserId = ""
                        Context.WorkingAsUserId = ""
                        Context.Timestamp = NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow)                        
                    }
                )
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
