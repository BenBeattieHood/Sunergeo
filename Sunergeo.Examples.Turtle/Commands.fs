namespace Sunergeo.Examples.Turtle

open Sunergeo.Core
open Sunergeo.Web

// Commands

[<Route("/turtle/create", HttpMethod.Put)>]
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

[<Route("/turtle/{TurtleId}/turn-left", HttpMethod.Post)>]
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
            
[<Route("/turtle/{TurtleId}/turn-right", HttpMethod.Post)>]
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

[<Route("/turtle/{TurtleId}/go-forwards", HttpMethod.Post)>]
type GoForwardsCommand = 
    {
        TurtleId: TurtleId
    }
    interface ICommand<TurtleId, Turtle, TurtleEvent> with 
        member this.GetId context = this.TurtleId
        member this.Exec context state =

            let canMoveForwards =
                match state.Position.X, state.Position.Y, state.Direction with
                | _, 100, Direction.North
                | 100, _, Direction.East
                | _, -100, Direction.South
                | -100, _, Direction.West ->
                    false
                | _ ->
                    true
                
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
            
[<Route("/turtle/{TurtleId}/set-visibility/{IsVisible}", HttpMethod.Post)>]
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