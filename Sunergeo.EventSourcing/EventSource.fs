namespace Sunergeo.EventSourcing

open Sunergeo.Core
open Sunergeo.KeyValueStorage

type EventSourceConfig<'State, 'Events> = {
    InstanceId: InstanceId
    Fold: 'State -> 'Events -> 'State
}

type EventSourceHead<'State> = {
    Position: int
    State: 'State option
}

type EventSourceError =
    Timeout
    | Disconnected

type EventSource<'State, 'Events, 'Id when 'Id : comparison>(config: EventSourceConfig<'State, 'Events>) = 
    let topicPath = 
        sprintf "%s.%s."
            typeof<'State>.Name
            config.InstanceId |> string

    let eventSource:Map<'Id, List<'Events>> = Map.empty /// TODO: replace with kafka

    let getPartition 
        (id:'Id)
        :List<'Events> option =
        eventSource
        |> Map.tryFind id

    member this.GetHead(id:'Id): Async<Result<EventSourceHead<'State>, EventSourceError>> =
        async { 
            return 
                FSharpx.Option.maybe {
                    let! partition = getPartition id
                    
                    let! head =
                        partition 
                        |> List.tryHead

                    return
                        {
                            Position = 0
                            State = None
                        }
                        |> Result.Ok
                }

        }

    member this.Add(event: 'Events, position: int): Result<unit, EventSourceError> =
        () |> Result.Ok
