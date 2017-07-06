namespace Sunergeo.Examples.Turtle.ReadStore

open Sunergeo.Core
open Sunergeo.Examples.Turtle.Core
open Sunergeo.Examples.Turtle.Events

module DefaultReadStore =

    type Turtle = {
        TurtleId: TurtleId
        Direction: Direction
        Position: Position list
        IsVisible: bool
    }
    
    let create
        (context: Context)
        (turtleId: TurtleId)
        : Turtle =
        {
            TurtleId = turtleId
            Direction = Direction.North
            Position = [{ X = 0; Y = 0 }]
            IsVisible = true
        }

    let fold
        (state: Turtle)
        (event: TurtleEvent)
        : Turtle =
        match event with
        | TurnedLeft event ->
            { state with
                Direction = state.Direction |> Utils.turnLeft
            }
        | TurnedRight event ->
            { state with
                Direction = state.Direction |> Utils.turnRight
            }
        | MovedForwards event ->
            { state with
                Position = 
                    state.Position
                    |> List.append
                        [(state.Position |> List.head, state.Direction) |> Utils.move]
            }
        | VisibilitySet event ->
            { state with
                IsVisible = event.IsVisible
            }



