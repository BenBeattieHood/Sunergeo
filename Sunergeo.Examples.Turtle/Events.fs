namespace Sunergeo.Examples.Turtle

open Sunergeo.Core

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