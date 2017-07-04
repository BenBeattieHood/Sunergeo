namespace Sunergeo.Examples.Turtle

open Sunergeo
open Sunergeo.Core
open Sunergeo.Web


// Aggregate id, just repurposing a string for now

type TurtleId = string


// Events

type CreatedEvent = 
    {
        TurtleId: TurtleId
    }
    interface IEvent

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
    | Created of CreatedEvent
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

module TurtleModule =
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

    let fold
        (state: Turtle option)
        (event: TurtleEvent)
        : Turtle =
        match event, state with
        | Created event, None ->
            {
                TurtleId = event.TurtleId
                Direction = Direction.North
                Position = { X = 0; Y = 0 }
                IsVisible = true
            }
        | _, Some state ->
            match event with
            | Created _ -> 
                failwith "Invalid case"
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
        | _, None -> failwith "Invalid case"


// Commands

[<Route("/turtle/create")>]
type CreateCommand = 
    {
        [<GeneratedId()>] 
        TurtleId: TurtleId
    }
    interface IUnvalidatedCommand<TurtleId, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
            seq {
                yield {
                    CreatedEvent.TurtleId = this.TurtleId
                } |> TurtleEvent.Created
            }
            |> Result.Ok

[<Route("/turtle/{_}/turn-left")>]
type TurnLeftCommand = 
    {
        TurtleId: TurtleId
    }
    interface IUnvalidatedCommand<TurtleId, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
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
    interface IUnvalidatedCommand<TurtleId, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
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
    interface IUnvalidatedCommand<TurtleId, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
            seq {
                yield {
                    MovedForwardsEvent.TurtleId = this.TurtleId
                } |> TurtleEvent.MovedForwards
            }
            |> Result.Ok
            
[<Route("/turtle/{_}/set-visibility/{_}")>]
type SetVisibilityCommand = 
    {
        TurtleId: TurtleId
        IsVisible: bool
    }
    interface IUnvalidatedCommand<TurtleId, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
            seq {
                yield {
                    VisibilitySetEvent.TurtleId = this.TurtleId
                    IsVisible = this.IsVisible
                } |> TurtleEvent.VisibilitySet
            }
            |> Result.Ok