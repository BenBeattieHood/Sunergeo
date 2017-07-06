namespace Sunergeo.Hosting

open System
open System.Reflection
open Sunergeo.Core
open Sunergeo.Web
open Sunergeo.Web.Commands

type Config = {
    Logger: Sunergeo.Logging.Logger
    BaseUri: Uri
    Assemblies: Assembly list
}

//type CommandHost<'Events>(config: Config) = 

//    let commands =
//        config.Assemblies
//        |> Seq.collect
//            (fun assembly ->
//                assembly.GetTypes()
//                |> Seq.where
//                    (fun t ->
//                        t.IsAssignableFrom(typedefof<ICommand<_, _, _>>)
//                    )
//            )
//        |> List.ofSeq

//    let routedCommands =
//        commands
//        |> List.choose
//            (fun command ->
//                command 
//                |> Reflection.getAttribute<RouteAttribute>
//                |> Option.map
//                    (fun routeAttribute ->
//                        { 
//                            RoutedCommand<>.PathAndQuery = routeAttribute.PathAndQuery
//                            RoutedType.CommandType = command
//                            RoutedType.HttpMethod = routeAttribute.HttpMethod
//                        }
//                    )
//            )
            
//    let commandWebHostConfig:Sunergeo.Web.Commands.CommandWebHostConfig = {
//        Logger = config.Logger
//        BaseUri = config.BaseUri
//        Commands = routedCommands
//    }

//    let commandWebHost = 
//        commandWebHostConfig
//        |> Sunergeo.Web.Commands.CommandWebHost.create

//    interface System.IDisposable with
//        member this.Dispose () =
//            commandWebHost.Dispose()
//            ()
