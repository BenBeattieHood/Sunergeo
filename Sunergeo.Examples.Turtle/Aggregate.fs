namespace Sunergeo.Examples.Turtle.Aggregate

open Sunergeo.Core
open Sunergeo.Examples.Turtle.Core
open Sunergeo.Examples.Turtle.Events
open Sunergeo.Examples.Turtle.State

// Events -> State

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Turtle =
    let create
        (context: Context)
        (turtleId: TurtleId)
        : Turtle =
        {
            TurtleId = turtleId
            Direction = Direction.North
            Position = { X = 0; Y = 0 }
            IsVisible = true
        }

    let fold
        (state: Turtle)
        (event: TurtleEvent)
        : Turtle =
        match event with
        | TurnedLeft event ->
            { state with
                Direction = state.Direction |> Utils.turnLeft
            }
        | TurnedRight event ->
            { state with
                Direction = state.Direction |> Utils.turnRight
            }
        | MovedForwards event ->
            { state with
                Position = (state.Position, state.Direction) |> Utils.move
            }
        | VisibilitySet event ->
            { state with
                IsVisible = event.IsVisible
            }