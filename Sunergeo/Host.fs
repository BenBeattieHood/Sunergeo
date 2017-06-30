namespace Sunergeo.Hosting


open System
open System.Reflection
open Sunergeo.Core
open Sunergeo.Web

type Config = {
    nothing: string
}

module HostModule =
    let getAttribute<'TAttribute when 'TAttribute :> Attribute>(t:Type):'TAttribute option =
        t.GetCustomAttributes(typeof<'TAttribute>)
        |> Seq.tryHead
        |> Option.map
            (fun (x) ->
                match x with
                | :? 'TAttribute as result -> Some result
                | _ -> None
            )
        |> Option.flatten

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
        BaseUri = Uri("http://localhost:8080")
        Commands = 
            commands
            |> List.choose
                (fun command ->
                    command 
                    |> HostModule.getAttribute<RouteAttribute>
                    |> Option.map
                        (fun routeAttribute ->
                            { 
                                Sunergeo.Web.WebHostRoutedCommand.Path = routeAttribute.Uri
                                CommandType = command
                            }
                        )
                )
    }

    let host = 
        config
        |> Sunergeo.Web.WebHost.create

    interface System.IDisposable with
        member this.Dispose () =
            host.Dispose()
            ()
