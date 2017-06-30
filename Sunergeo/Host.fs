namespace Sunergeo.Hosting


open System
open System.Reflection
open Sunergeo.Core
open Sunergeo.Web

type Config = {
    nothing: string
}

type Host(config: Config, assemblies: Assembly[]) = 

    let commands =
        assemblies
        |> Seq.collect
            (fun assembly ->
                assembly.GetTypes()
                |> Seq.where
                    (fun t ->
                        t.IsAssignableFrom(typedefof<ICommand<_, _>>)
                    )
            )
        |> List.ofSeq
            
    let config:Sunergeo.Web.WebHostConfig = {
        Logger = None
        Commands = 
            commands
            |> List.choose
                (fun command ->
                    
                    command.GetCustomAttributes(typeof<RouteAttribute>)
                    |> Seq.tryHead
                    |> Option.map
                        (fun (routeAttribute :?> RouteAttribute) ->
                            routeAttribute.Uri
                        )
                    | routeAttribute :: _ ->
                        Some routeAttribute
                )
    }

    let host = Sunergeo.Web.WebHost.create
    let temp = WebApp.Start<HelloWorld> ("http://localhost:7000")
    ()

    interface System.IDisposable with
        member this.Dispose () =
            actorSystem.Dispose()
            ()
