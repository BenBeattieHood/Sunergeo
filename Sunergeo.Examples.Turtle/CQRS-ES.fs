namespace Sunergeo.Examples.Turtle

open Sunergeo
open Sunergeo.Core
open Sunergeo.Web


// Aggregate id, just repurposing a string for now

type TurtleId = string


// Events

type TurnedLeftEvent = 
    {
        TurtleId: TurtleId
    }
    interface IEvent
    
type TurnedRightEvent = 
    {
        TurtleId: TurtleId
    }
    interface IEvent
    
type MovedForwardsEvent = 
    {
        TurtleId: TurtleId
    }
    interface IEvent
    
type VisibilitySetEvent = 
    {
        TurtleId: TurtleId
        IsVisible: bool
    }
    interface IEvent

// Events DU

type TurtleEvent =
    | TurnedLeft of TurnedLeftEvent
    | TurnedRight of TurnedRightEvent
    | MovedForwards of MovedForwardsEvent
    | VisibilitySet of VisibilitySetEvent

// State

type Direction = 
    | North
    | East
    | South
    | West

type Position = {
    X: int
    Y: int
}

type Turtle = {
    TurtleId: TurtleId
    Direction: Direction
    Position: Position
    IsVisible: bool
}

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


// Commands

[<Route("/turtle/create")>]
type CreateCommand = 
    {
        [<GeneratedId()>] 
        TurtleId: TurtleId
    }
    interface ICreateCommand<TurtleId, Turtle, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
            (
                this.TurtleId |> Turtle.create context,
                Seq.empty
            )
            |> Result.Ok

[<Route("/turtle/{_}/turn-left")>]
type TurnLeftCommand = 
    {
        TurtleId: TurtleId
    }
    interface ICommand<TurtleId, Turtle, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context _ =
            seq {
                yield {
                    TurnedLeftEvent.TurtleId = this.TurtleId
                } |> TurtleEvent.TurnedLeft
            }
            |> Result.Ok
            
[<Route("/turtle/{_}/turn-right")>]
type TurnRightCommand = 
    {
        TurtleId: TurtleId
    }
    interface ICommand<TurtleId, Turtle, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context _ =
            seq {
                yield {
                    TurnedRightEvent.TurtleId = this.TurtleId
                } |> TurtleEvent.TurnedRight
            }
            |> Result.Ok

[<Route("/turtle/{_}/go-forwards")>]
type GoForwardsCommand = 
    {
        TurtleId: TurtleId
    }
    interface ICommand<TurtleId, Turtle, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context state =
            let min, max = -100, 100

            let canMoveForwards =
                match (state.Position.X, state.Position.Y, state.Direction) with
                | (_, max, Direction.North) -> false
                | max, _, Direction.East -> false
                | _, min, Direction.South -> false
                | min, _, Direction.West -> false
                | _ -> true
                
            if canMoveForwards 
            then
                seq {
                    yield {
                        MovedForwardsEvent.TurtleId = this.TurtleId
                    } |> TurtleEvent.MovedForwards
                }
                |> Result.Ok
            else
                "Turtle would move outside boundary"
                |> Error.InvalidOp
                |> Result.Error
            
[<Route("/turtle/{_}/set-visibility/{_}")>]
type SetVisibilityCommand = 
    {
        TurtleId: TurtleId
        IsVisible: bool
    }
    interface ICommand<TurtleId, Turtle, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context _ =
            seq {
                yield {
                    VisibilitySetEvent.TurtleId = this.TurtleId
                    IsVisible = this.IsVisible
                } |> TurtleEvent.VisibilitySet
            }
            |> Result.Ok