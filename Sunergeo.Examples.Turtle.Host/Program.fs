﻿// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open Sunergeo.Core
open Sunergeo.EventSourcing
open Sunergeo.KeyValueStorage
open Sunergeo.Logging
open Sunergeo.Web
open Sunergeo.Web.Commands
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

    let snapshotStoreConfig:KeyValueStorageConfig = 
        {
            Uri = Uri("localhost:3000")
            Logger = logger
            TableName = instanceId |> Utils.toTopic<Sunergeo.Examples.Turtle.Turtle>
        }

    use snapshotStore = new KeyValueStore<Sunergeo.Examples.Turtle.TurtleId, Snapshot<Sunergeo.Examples.Turtle.Turtle>>(snapshotStoreConfig)

    let eventSourceConfig:EventSourceConfig<Sunergeo.Examples.Turtle.TurtleId, Sunergeo.Examples.Turtle.Turtle, Sunergeo.Examples.Turtle.TurtleEvent> = 
        {
            InstanceId = instanceId
            Fold = Sunergeo.Examples.Turtle.Turtle.fold
            SnapshotStore = snapshotStore
            LogUri = Uri("localhost:9092")
            Logger = logger
        }
    
    use eventSource = new Sunergeo.EventSourcing.EventSource<Sunergeo.Examples.Turtle.TurtleId, Sunergeo.Examples.Turtle.Turtle, Sunergeo.Examples.Turtle.TurtleEvent>(eventSourceConfig)

    let commandWebHostConfig:CommandWebHostConfig<Sunergeo.Examples.Turtle.TurtleEvent> = 
        {
            Logger = logger
            BaseUri = Uri("http://localhost:8080")
            Commands = 
                [
                    //PathAndQuery: string
                    //HttpMethod: HttpMethod
                    //Exec: 'TargetType -> Microsoft.AspNetCore.Http.HttpRequest -> Result<'Result, Error>
                    //                RoutedCommand<CreateCommand, TurtleEvent>.PathA
                ]
                |> List.map CommandWebHost.toGeneralRoutedCommand
            OnHandle = 
                (fun events ->
                    //events
                    //|> eventSource.Exec
                    ()
                )
        }

    use commandWebHost = 
        commandWebHostConfig
        |> CommandWebHost.create

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

    System.Console.ReadLine() |> ignore


    0 // return an integer exit code