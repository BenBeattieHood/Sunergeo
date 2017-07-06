namespace Sunergeo.Examples.Turtle

open Sunergeo.Core

// Events DU

type TurtleEvent =
    | TurnedLeft of TurnedLeftEvent
    | TurnedRight of TurnedRightEvent
    | MovedForwards of MovedForwardsEvent
    | VisibilitySet of VisibilitySetEvent

// Events -> State

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Turtle =
    let turnRight
        (direction:Direction)
        :Direction =
        match direction with
        | North -> East
        | East -> South
        | South -> West
        | West -> North
        
    let turnLeft
        (direction:Direction)
        :Direction =
        match direction with
        | North -> West
        | West -> South
        | South -> East
        | East -> North

    let move
        (
            position: Position,
            direction: Direction
        )
        :Position =
        {
            Position.X = 
                position.X +
                match direction with
                | East -> 1
                | West -> -1
                | _ -> 0
                
            Position.Y = 
                position.Y +
                match direction with
                | North -> 1
                | South -> -1
                | _ -> 0
        }

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
                Direction = state.Direction |> turnLeft
            }
        | TurnedRight event ->
            { state with
                Direction = state.Direction |> turnRight
            }
        | MovedForwards event ->
            { state with
                Position = (state.Position, state.Direction) |> move
            }
        | VisibilitySet event ->
            { state with
                IsVisible = event.IsVisible
            }