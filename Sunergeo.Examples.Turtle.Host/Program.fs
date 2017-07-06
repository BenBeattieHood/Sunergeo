// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open Sunergeo.EventSourcing
open Sunergeo.Logging
open Sunergeo.Web
open Sunergeo.Web.Commands
open Sunergeo.Examples.Turtle
open ResultModule

[<EntryPoint>]
let main argv = 
    let assemblies = [typeof<Sunergeo.Examples.Turtle.Turtle>.Assembly]
    let config:CommandWebHostConfig = {
        Logger = 
            (fun (logLevel: LogLevel) (message: string) ->
                System.Console.WriteLine message
            )
        BaseUri = Uri("http://localhost:8080")
        Commands = []
    }

    //use host = 
    //    config
    //    |> CommandWebHost.create

    let logConfig:LogConfig = {
        Uri = "localhost:9092"
        Topic = "turtle"
    }

    let logTopic = new LogTopic<int, Object>(logConfig)

    let evt = TurnedLeft {
        TurtleId = Guid.NewGuid().ToString()
    }

    let asyncResult = 
        async {
            let! result1 = logTopic.Add(0, evt)
            match result1 with
            | Result.Ok offset -> printfn "%i" offset
            | Result.Error e -> printf "Error of some kind"

            return result1
        }
    
    let addEventResult: Result<int64, LogError> = 
        asyncResult
        |> Async.RunSynchronously

    match addEventResult with
    | Result.Error _ -> printf "Result is error" |> ignore
    | Result.Ok offset ->
                    let outerResult2 = 
                        async {
                            let! readResult = logTopic.ReadFrom(0, offset)
                            match readResult with
                            | Result.Ok r -> printfn "got %d entries" (Seq.length r)
                            | Result.Error _ -> printfn "error"
                            return readResult
                        }
    
                    let result2 = 
                        outerResult2
                        |> Async.RunSynchronously 
                    printf "hello world" |> ignore

    System.Console.ReadLine() |> ignore


    0 // return an integer exit code
