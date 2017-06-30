namespace Sunergeo.EventSourcing

type EventSourceConfig = {
    uri: string // placeholder
}

type EventSourceHead<'TState> = {
    
}

module EventSourceModule =
    

type EventSource<'TState>(config: EventSourceConfig) = 
    member this.GetHead() =
        
