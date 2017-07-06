namespace Sunergeo.Examples.Turtle

open Sunergeo.Core

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