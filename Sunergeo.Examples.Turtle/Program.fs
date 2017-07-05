// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System
open Sunergeo.Hosting
open Sunergeo.Logging

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
        Host(config)
    0 // return an integer exit code
