namespace Sunergeo.EventSourcing

type EventSourceConfig = {
    uri: string // placeholder
}

type EventSourceHead<'TState> = {
    Position: int
    State: 'TState
}

type EventSourceError =
    Timeout
    | Disconnected

type EventSource<'TState, 'TEvents>(config: EventSourceConfig) = 
    member this.GetHead(): Result<EventSourceHead<'TState>, EventSourceError> =
        {
            Position = 0
            State = Unchecked.defaultof<'TState>
        }
        |> Result.Ok

    member this.Add(event: 'TEvents, position: int): Result<unit, EventSourceError> =
        () |> Result.Ok
