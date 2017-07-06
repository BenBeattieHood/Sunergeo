namespace Sunergeo.Examples.Turtle.Core

open Sunergeo.Core

// Aggregate id, just repurposing a string for now

type TurtleId = string

type Direction = 
    | North
    | East
    | South
    | West

type Position = {
    X: int
    Y: int
}

module Utils =
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
