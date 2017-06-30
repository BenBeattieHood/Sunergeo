namespace Sunergeo.Examples.Turtle

open Sunergeo
open Sunergeo.Core
open Sunergeo.Web


// Aggregate id, just repurposing a string for now

type TurtleId = string


// Events

type ITurtleEvent = IEvent

type CreatedEvent = 
    {
        TurtleId: TurtleId
    }
    interface ITurtleEvent

type TurnedLeftEvent = 
    {
        TurtleId: TurtleId
    }
    interface ITurtleEvent
    
type TurnedRightEvent = 
    {
        TurtleId: TurtleId
    }
    interface ITurtleEvent
    
type MovedForwardsEvent = 
    {
        TurtleId: TurtleId
    }
    interface ITurtleEvent
    
type VisibilityChangedEvent = 
    {
        TurtleId: TurtleId
        IsVisible: bool
    }
    interface ITurtleEvent

// Events DU

type TurtleEvent =
    | Created of CreatedEvent
    | TurnedLeft of TurnedLeftEvent
    | TurnedRight of TurnedRightEvent
    | MovedForwards of MovedForwardsEvent
    | VisibilityChanged of VisibilityChangedEvent

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

    let appendEvent
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
            | VisibilityChanged event ->
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
    interface IUnvalidatedCommand<ITurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
            seq<ITurtleEvent> {
                yield upcast {
                    TurnedLeftEvent.TurtleId = this.TurtleId
                } 
            }
            |> Microsoft.FSharp.Core.Result.Ok

[<Route("/turtle/turn-left")>]
type TurnLeftCommand = 
    {
        TurtleId: TurtleId
    }
    interface IUnvalidatedCommand<ITurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
            seq<ITurtleEvent> {
                yield upcast {
                    TurnedLeftEvent.TurtleId = this.TurtleId
                } 
            }
            |> Microsoft.FSharp.Core.Result.Ok
            
[<Route("/turtle/turn-right")>]
type TurnRightCommand = 
    {
        TurtleId: TurtleId
    }
    interface IUnvalidatedCommand<ITurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
            seq<ITurtleEvent> {
                yield upcast {
                    TurnedLeftEvent.TurtleId = this.TurtleId
                } 
            }
            |> Microsoft.FSharp.Core.Result.Ok

[<Route("/turtle/go-forwards")>]
type GoForwardsCommand = 
    {
        TurtleId: TurtleId
    }
    interface IUnvalidatedCommand<ITurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context =
            seq<ITurtleEvent> {
                yield upcast {
                    MovedForwardsEvent.TurtleId = this.TurtleId
                } 
            }
            |> Microsoft.FSharp.Core.Result.Ok
