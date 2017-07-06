module Reflection

open System
open System.Reflection

let getAttribute<'TAttribute when 'TAttribute :> Attribute>(t:Type):'TAttribute option =
    t.GetCustomAttributes(typeof<'TAttribute>)
    |> Seq.tryHead
    |> Option.map
        (fun (x) ->
            match x with
            | :? 'TAttribute as result -> Some result
            | _ -> None
        )
    |> Option.flatten
        