namespace Sunergeo.EventSourcing

open Sunergeo.Core
open Sunergeo.KeyValueStorage

type Snapshot<'State> = {
    Position: int
    State: 'State
}

type EventSourceConfig<'PartitionId, 'State, 'Events when 'PartitionId : comparison> = {
    InstanceId: InstanceId
    Fold: 'State -> 'Events -> 'State
    SnapshotStore: Sunergeo.KeyValueStorage.KeyValueStore<'PartitionId, Snapshot<'State>>
    LogUri: string
}

type LogEntry<'Item> = {
    Position: int
    Item: 'Item
}

type EventSourceError =
    Timeout
    | Disconnected

type LogConfig = {
    Uri: string // placeholder
    Topic: string
}

type LogTopic<'PartitionId, 'Item when 'PartitionId : comparison>(config: LogConfig) =
    
    let mutable eventSource:Map<'PartitionId, LogEntry<'Item> seq> = Map.empty /// TODO: replace with kafka
    let toAsync (a:'a): Async<'a> = async { return a }

    member this.Add(partitionId: 'PartitionId, item: 'Item): Async<int> =
        async {
            let partition =
                eventSource
                |> Map.tryFind partitionId
                |> Option.defaultValue Seq.empty

            let partitionLength = 
                partition
                |> Seq.length
            
            eventSource <- 
                eventSource 
                |> Map.add 
                    partitionId 
                    (
                        partition 
                        |> Seq.append [ { Position = partitionLength; Item = item } ]
                    )

            return partitionLength
        }

    member this.ReadFrom(partitionId: 'PartitionId, positionId: int): Async<LogEntry<'Item> seq> =
        async {
            let result =
                eventSource
                |> Map.tryFind partitionId
                |> function
                    | Some items ->
                        items
                        |> Seq.skip positionId
                    | None ->
                        upcast [] 

            return result
        }

    member this.ReadLast(partitionId: 'PartitionId): Async<LogEntry<'Item> option> =
        async {
            let result =
                eventSource
                |> Map.tryFind partitionId
                |> Option.map
                    (fun x -> x |> Seq.last)

            return result
        }
        
        

type EventSource<'State, 'Events, 'PartitionId when 'PartitionId : comparison>(config: EventSourceConfig<'PartitionId, 'State, 'Events>) = 
    let topic = 
        sprintf "%s.%s"
            typeof<'State>.Name
            config.InstanceId |> string

    let logConfig:LogConfig = {
        Topic = topic
        Uri = config.LogUri
    }

    let kafkaTopic = LogTopic<'PartitionId, 'Events>(logConfig)

    let addWith
        (stateAndVersion: ('Value * KeyValueVersion) option)
        :Async<unit> =
        async {
            return ()
        }
    
    member this.ReadFrom(partitionId: 'PartitionId, positionId: int): Async<Result<'Events seq, EventSourceError>> = 
        async {
            let! entries = kafkaTopic.ReadFrom(partitionId, positionId)

            return 
                entries 
                |> Seq.map (fun x -> x.Item) 
                |> Result.Ok
        }

    member this.Add(partitionId: 'PartitionId, event: 'Events, position: int):Async<Result<unit, EventSourceError>> =
        async {
            let! snapshot = config.SnapshotStore.Get partitionId
            return 
                ResultModule.bimap
                    (fun stateAndVersion ->
                        ()
                    )
                    (fun error ->
                        Sunergeo.Core.Todo.todo()
                    )
        }