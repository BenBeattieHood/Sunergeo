namespace Sunergeo.Examples.Turtle.State

open Sunergeo.Examples.Turtle.Core

// State

type Turtle = {
    TurtleId: TurtleId
    Direction: Direction
    Position: Position
    IsVisible: bool
}