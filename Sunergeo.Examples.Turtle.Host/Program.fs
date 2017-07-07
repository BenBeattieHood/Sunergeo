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

open ResultModule

open Sunergeo.Examples.Turtle.Core
open Sunergeo.Examples.Turtle.Events
open Sunergeo.Examples.Turtle.State
open Sunergeo.Examples.Turtle.Aggregate
open Sunergeo.Examples.Turtle.Commands
open Microsoft.AspNetCore.Http

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
            Fold = Turtle.fold
            SnapshotStore = snapshotStore
            LogUri = Uri("localhost:9092")
            Logger = logger
        }
    
    use eventSource = new Sunergeo.EventSourcing.EventSource<TurtleId, Turtle, TurtleEvent>(eventSourceConfig)

    sprintf "Connected to kafka : %O" eventSourceConfig.LogUri
    |> Console.WriteLine
    
//type RoutedType<'TargetType, 'Result> = {
//    PathAndQuery: string
//    HttpMethod: HttpMethod
//    Exec: 'TargetType -> Microsoft.AspNetCore.Http.HttpRequest -> Result<'Result, Error>
//}RoutedType<'Command, CommandResult<'State, 'Events>>

    let commandWebHostConfig:CommandWebHostConfig<State.Turtle, TurtleEvent> = 
        {
            Logger = logger
            BaseUri = Uri("http://localhost:8080")
            Commands = 
                [
                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<CreateCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<CreateCommand>).Value.HttpMethod
                        RoutedCommand.Exec = 
                            (fun (command: CreateCommand) (context: Context) (request: HttpRequest) ->
                                Sunergeo.Core.Todo.todo()
                                //(command :> ICreateCommand).Exec context
                            )
                    }
                    
                    //{
                    //    RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<TurnLeftCommand>).Value.PathAndQuery
                    //    RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<TurnLeftCommand>).Value.HttpMethod
                    //    RoutedCommand.Exec = 
                    //        (fun (command: TurnLeftCommand) (context: Context) ->
                    //            Sunergeo.Core.Todo.todo()
                    //            //(command :> ICreateCommand).Exec context
                    //        )
                    //}

                    //{
                    //    RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<TurnRightCommand>).Value.PathAndQuery
                    //    RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<TurnRightCommand>).Value.HttpMethod
                    //    RoutedCommand.Exec = 
                    //        (fun (command: TurnRightCommand) (context: Context) ->
                    //            Sunergeo.Core.Todo.todo()
                    //            //(command :> ICreateCommand).Exec context
                    //        )
                    //}

                    //{
                    //    RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<MovedForwardsCommand>).Value.PathAndQuery
                    //    RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<MovedForwardsCommand>).Value.HttpMethod
                    //    RoutedCommand.Exec = 
                    //        (fun (command: MovedForwardsCommand) (context: Context) ->
                    //            Sunergeo.Core.Todo.todo()
                    //            //(command :> ICreateCommand).Exec context
                    //        )
                    //}
                ]
                |> List.map CommandWebHost.toGeneralRoutedCommand
            OnHandle = 
                (fun result ->
                    match result with
                    | CommandResult.Create (state, events) ->
                        Console.WriteLine("Create OnHandle called...")
                        ()
                    | CommandResult.Update events ->
                        ()
                    //events
                    //|> eventSource.Exec
                    ()
                )
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
