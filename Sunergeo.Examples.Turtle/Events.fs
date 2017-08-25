namespace Sunergeo.Examples.Turtle.Events

open Sunergeo.Core
open Sunergeo.Examples.Turtle.Core

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



// Init

type TurtleInit = unit