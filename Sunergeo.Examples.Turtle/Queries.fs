namespace Sunergeo.Examples.Turtle.Queries

open Sunergeo.Core
open Sunergeo.Web
open Sunergeo.KeyValueStorage
open Sunergeo.Examples.Turtle.ReadStore

open Sunergeo.Examples.Turtle.Core
open Sunergeo.Examples.Turtle.State
open Sunergeo.Examples.Turtle.Aggregate

[<Route("/turtle/{TurtleId}", HttpMethod.Get)>]
type GetTurtle<'KeyValueVersion when 'KeyValueVersion : comparison> = 
    {
        TurtleId: TurtleId
    }
    interface IQuery<IReadOnlyKeyValueStore<TurtleId, Snapshot<DefaultReadStore.Turtle>, 'KeyValueVersion>, DefaultReadStore.Turtle option> with 
        member this.Exec context readStore =
            async {
                return 
                    readStore.Get this.TurtleId
                    |> ResultModule.bimap
                        (Option.map (fst >> Snapshot.state))
                        (function
                            | ReadError.Timeout -> "timeout" |> Error.InvalidOp
                            | ReadError.Error s -> s |> Error.InvalidOp
                        )
            }

