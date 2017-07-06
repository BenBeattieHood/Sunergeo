// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open Sunergeo.EventSourcing
open Sunergeo.Logging
open Sunergeo.Hosting
open Sunergeo.Examples.Turtle

[<EntryPoint>]
let main argv = 
    let assemblies = [typeof<Sunergeo.Examples.Turtle.Turtle>.Assembly]
    let config:Config = {
        Logger = 
            (fun (logLevel: LogLevel) (message: string) ->
                System.Console.WriteLine message
            )
        BaseUri = Uri("http://localhost:8080")
        Assemblies = assemblies
    }

    use host = 
        CommandHost(config)

    let logConfig:LogConfig = {
        Uri = "localhost:9092"
        Topic = "turtle"
    }

    let logTopic = new LogTopic<int, Object>(logConfig)

    let evt = TurnedLeft {
        TurtleId = Guid.NewGuid().ToString()
    }

    let outerResult = async {
        let! result1 = logTopic.Add(0, evt)
        match result1 with
        | Result.Ok offset -> printfn "%i" offset
        | Result.Error e -> printf "Error of some kind"
        result1
    }
    
    Async.RunSynchronously outerResult

    System.Console.ReadLine() |> ignore

    0 // return an integer exit code
