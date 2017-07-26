﻿// Learn more about F# at http://fsharp.org
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
open Microsoft.AspNetCore.Http

let execCreateCommandFor<'PartitionId, 'Init, 'State, 'Events, 'Command, 'KeyValueVersion when 'Command :> ICreateCommand<'PartitionId, 'State, 'Events> and 'PartitionId : comparison and 'KeyValueVersion : comparison>
    (eventStore: Sunergeo.EventSourcing.EventStore<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion>)
    (command: 'Command)
    (context: Context)
    (request: HttpRequest)
    : Result<unit, Error> =
    eventStore.Create
        context
        (command.GetId context)
        command.Exec
    |> Async.RunSynchronously

let execCommandFor<'PartitionId, 'Init, 'State, 'Events, 'Command, 'KeyValueVersion when 'Command :> IUpdateCommand<'PartitionId, 'State, 'Events> and 'PartitionId : comparison and 'KeyValueVersion : comparison>
    (eventStore: Sunergeo.EventSourcing.EventStore<'PartitionId, 'Init, 'State, 'Events, 'KeyValueVersion>)
    (command: IUpdateCommand<'PartitionId, 'State, 'Events>)
    (context: Context)
    (request: HttpRequest)
    : Result<unit, Error> =
    eventStore.Append
        context
        (command.GetId context)
        command.Exec
    |> Async.RunSynchronously

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

    //let snapshotStoreConfig:KeyValueStorageConfig = 
    //    {
    //        Uri = Uri("localhost:3000")
    //        Logger = logger
    //        TableName = instanceId |> Utils.toTopic<Turtle>
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
                        RoutedCommand.Exec = execCreateCommand
                    } |> Routing.createHandler
                    
                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<TurnLeftCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<TurnLeftCommand>).Value.HttpMethod
                        RoutedCommand.Exec = execCommand
                    } |> Routing.createHandler

                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<TurnRightCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<TurnRightCommand>).Value.HttpMethod
                        RoutedCommand.Exec = execCommand
                    } |> Routing.createHandler

                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<GoForwardsCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<GoForwardsCommand>).Value.HttpMethod
                        RoutedCommand.Exec = execCommand
                    } |> Routing.createHandler
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
            Handlers = 
                [
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
