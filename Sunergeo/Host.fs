namespace Sunergeo.Hosting


open System
open System.Reflection
open Sunergeo

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
            

    let temp = WebApp.Start<HelloWorld> ("http://localhost:7000")
    ()

    interface System.IDisposable with
        member this.Dispose () =
            actorSystem.Dispose()
            ()
