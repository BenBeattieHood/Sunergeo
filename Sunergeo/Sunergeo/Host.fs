namespace Sunergeo

type Config = {
    nothing: string
}

open System
open System.Reflection

open Orleankka             // base types of Orleankka
//open Orleankka.FSharp      // additional API layer for F#
open Orleankka.Playground  // default host configuration

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
    let actorSystem = 
        ActorSystem
            .Configure()
            .Playground()
            .Register(assemblies)
            .Done()

    member this.X () = 
        open Freya.Core
        open Freya.Machines.Http
        open Freya.Routers.Uri.Template

        let name =
            freya {
                let! name = Freya.Optic.get (Route.atom_ "name")

                match name with
                | Some name -> return name
                | _ -> return "World" }

        let hello =
            freya {
                let! name = name

                return Represent.text (sprintf "Hello %s!" name) }

        let machine =
            freyaMachine {
                handleOk hello }

        let router =
            freyaRouter {
                resource "/hello{/name}" machine }

        type HelloWorld () =
            member __.Configuration () =
                OwinAppFunc.ofFreya (router)

        open System
        open Microsoft.Owin.Hosting

        let temp = WebApp.Start<HelloWorld> ("http://localhost:7000")
        ()

    interface System.IDisposable with
        member this.Dispose () =
            actorSystem.Dispose()
            ()
