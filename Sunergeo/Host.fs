namespace Sunergeo.Hosting


open System
open System.Reflection
open Sunergeo.Core
open Sunergeo.Web

type Config = {
    Logger: Sunergeo.Logging.Logger
    BaseUri: Uri
    Assemblies: Assembly list
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

type Host(config: Config) = 

    let commands =
        config.Assemblies
        |> Seq.collect
            (fun assembly ->
                assembly.GetTypes()
                |> Seq.where
                    (fun t ->
                        t.IsAssignableFrom(typedefof<ICommand<_, _, _>>)
                    )
            )
        |> List.ofSeq

    let commandsWithRoutes =
        commands
        |> List.choose
            (fun command ->
                command 
                |> HostModule.getAttribute<RouteAttribute>
                |> Option.map
                    (fun routeAttribute ->
                        { 
                            WebHostRoutedCommand.PathAndQuery = routeAttribute.PathAndQuery
                            WebHostRoutedCommand.CommandType = command
                            WebHostRoutedCommand.HttpMethod = routeAttribute.HttpMethod
                        }
                    )
            )
            
    let webHostConfig:Sunergeo.Web.WebHostConfig = {
        Logger = config.Logger
        BaseUri = config.BaseUri
        Commands = 
            commandsWithRoutes
            |> List.map
                (fun (command, uri) ->
                    { 
                        Sunergeo.Web.WebHostRoutedCommand.PathAndQuery = uri
                        CommandType = command
                        HttpMethod = 
                    }
                )
    }

    let webHost = 
        webHostConfig
        |> Sunergeo.Web.WebHost.create

    interface System.IDisposable with
        member this.Dispose () =
            webHost.Dispose()
            ()
