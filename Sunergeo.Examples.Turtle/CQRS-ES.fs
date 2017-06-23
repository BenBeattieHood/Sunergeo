namespace Sunergeo.Examples.Turtle

open Sunergeo

type TurtleId = string


// Events

type ITurtleEvent = IEvent

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


// Commands

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
