namespace Sunergeo.Projection

open Sunergeo.Core

open System.Reflection

open Orleankka                       // base types of Orleankka
open Orleankka.FSharp                // additional API layer for F#
open Orleankka.FSharp.Runtime        // Actor base class defined here

// https://github.com/OrleansContrib/Orleankka/wiki/Getting-Started-F%23-(ver-1.0)

type Projector<'Events, 'State>(create: ICreatedEvent -> 'State, fold: 'State -> 'Events -> 'State) =
    inherit Actor<'Events>()

    let mutable state: 'State option = None

    override this.Receive event = 
        task {
            let newState = 
                match event with
                | :? ICreatedEvent as createdEvent -> 
                    match state with
                    | None -> 
                        create event
                    | Some state ->
                        failwith (sprintf "Invalid state (%O) to apply create event onto" state)

                | _ ->
                    event |> fold state

            state <- newState
            return nothing
        }

type ProjectionHostConfig = {
    assemblies: Assembly list
}

type ProjectionHost(config: ProjectionHostConfig) = 
    let 
    member this.X = "F#"
