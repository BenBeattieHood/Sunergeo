﻿// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open Sunergeo.Core
open Sunergeo.EventSourcing
open Sunergeo.KeyValueStorage
open Sunergeo.Logging
open Sunergeo.Web
open Sunergeo.Web.Commands
open Sunergeo.Web.Queries
open Sunergeo.Examples.Turtle
open ResultModule

[<EntryPoint>]
let main argv = 
    //let assemblies = [typeof<Sunergeo.Examples.Turtle.Turtle>.Assembly]

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
            TableName = instanceId |> Utils.toTopic<Sunergeo.Examples.Turtle.Turtle>
        }

    use snapshotStore = new KeyValueStore<Sunergeo.Examples.Turtle.TurtleId, Snapshot<Sunergeo.Examples.Turtle.Turtle>>(snapshotStoreConfig)

    sprintf "Connected to snapshot store : %O" snapshotStoreConfig.Uri
    |> Console.WriteLine

    let eventSourceConfig:EventSourceConfig<Sunergeo.Examples.Turtle.TurtleId, Sunergeo.Examples.Turtle.Turtle, Sunergeo.Examples.Turtle.TurtleEvent> = 
        {
            InstanceId = instanceId
            Fold = Sunergeo.Examples.Turtle.Turtle.fold
            SnapshotStore = snapshotStore
            LogUri = Uri("localhost:9092")
            Logger = logger
        }
    
    use eventSource = new Sunergeo.EventSourcing.EventSource<Sunergeo.Examples.Turtle.TurtleId, Sunergeo.Examples.Turtle.Turtle, Sunergeo.Examples.Turtle.TurtleEvent>(eventSourceConfig)

    sprintf "Connected to kafka : %O" eventSourceConfig.LogUri
    |> Console.WriteLine
    
//type RoutedType<'TargetType, 'Result> = {
//    PathAndQuery: string
//    HttpMethod: HttpMethod
//    Exec: 'TargetType -> Microsoft.AspNetCore.Http.HttpRequest -> Result<'Result, Error>
//}RoutedType<'Command, CommandResult<'State, 'Events>>

    let commandWebHostConfig:CommandWebHostConfig<Sunergeo.Examples.Turtle.Turtle, Sunergeo.Examples.Turtle.TurtleEvent> = 
        {
            Logger = logger
            BaseUri = Uri("http://localhost:8080")
            Commands = 
                [
                    {
                        RoutedCommand.PathAndQuery = (Reflection.getAttribute<RouteAttribute> typeof<CreateCommand>).Value.PathAndQuery
                        RoutedCommand.HttpMethod = (Reflection.getAttribute<RouteAttribute> typeof<CreateCommand>).Value.HttpMethod
                        RoutedCommand.Exec = 
                            (fun (command: CreateCommand) ->
                                (command :> ICreateCommand).Exec
                            )
                    }
                ]
                |> List.map CommandWebHost.toGeneralRoutedCommand
            OnHandle = 
                (fun result ->
                    match result with
                    | CommandResult.Create (state, events) ->
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

    sprintf "Serving commands : %O" commandWebHostConfig.BaseUri
    |> Console.WriteLine

    let queryWebHostConfig:QueryWebHostConfig = 
        {
            Logger = logger
            Queries = 
                [
                ]
                |> List.map QueryWebHost.toGeneralRoutedQuery
            BaseUri = Uri("http://localhost:8081")
        }

    use queryWebHost =
        queryWebHostConfig
        |> QueryWebHost.create

    sprintf "Serving queries : %O" queryWebHostConfig.BaseUri
    |> Console.WriteLine
    
    sprintf "Servers ready. Press enter to quit..."
    |> Console.WriteLine

    System.Console.ReadLine() |> ignore
    
    0 // return an integer exit code
