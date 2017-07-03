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
    Logger: Sunergeo.Logging.Logger
}

type LogEntry<'Item> = {
    Position: int   // in Kafka this is called the offset, and it is available after the item has been written to a partition
    Item: 'Item
}

type LogConfig = {
    Uri: string // placeholder
    Topic: string
}

type LogError =
    Timeout
    | Error of string

type LogTransactionId = string

type LogTopic<'PartitionId, 'Item when 'PartitionId : comparison>(config: LogConfig) =
    
    let mutable eventSource:Map<'PartitionId, LogEntry<'Item> seq> = Map.empty /// TODO: replace with kafka with enable.idempotence=true
    let toAsync (a:'a): Async<'a> = async { return a }

    // https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
    member this.BeginTransaction(): Async<Result<LogTransactionId, LogError>> =
        async {
            return ("" : LogTransactionId) |> Result.Ok
        }

    member this.AbortTransaction(): Async<Result<unit, LogError>> =
        async {
            return Sunergeo.Core.Todo.todo()
        }

    member this.CommitTransaction(): Async<Result<unit, LogError>> =
        async {
            return Sunergeo.Core.Todo.todo()
        }

    member this.Add(partitionId: 'PartitionId, item: 'Item): Async<Result<int, LogError>> =
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

            return partitionLength |> Result.Ok
        }

    member this.ReadFrom(partitionId: 'PartitionId, positionId: int): Async<Result<LogEntry<'Item> seq, LogError>> =
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

            return result |> Result.Ok
        }

    member this.ReadLast(partitionId: 'PartitionId): Async<Result<LogEntry<'Item> option, LogError>> =
        async {
            let result =
                eventSource
                |> Map.tryFind partitionId
                |> Option.map
                    (fun x -> x |> Seq.last)

            return result |> Result.Ok
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

    let createCommandWrapper
        (context: Context)
        (command: ICommandBase<'PartitionId>)
        : ('State option -> Result<'Events seq, Error>) =

        match command with
        | :? ICreateCommand<'PartitionId, 'Events> as createCommand ->
            function
            | None -> createCommand.Exec context
            | Some state -> failwith "Create command must have empty state"

        | :? ICommand<'PartitionId, 'Events, 'State> as command ->
            function
            | Some state -> command.Exec context state
            | None -> failwith "Command must have state"

        | :? IUnvalidatedCommand<'PartitionId, 'Events> as unvalidatedCommand ->
            (fun _ -> unvalidatedCommand.Exec context)

        | _ ->
            failwith "Unrecognised command type"

    let folder 
        (asyncResult: Async<Result<int, LogError>> option)
        (event: 'Events)
        : Async<Result<int, LogError>> option =
        
        Sunergeo.Core.Todo.todo()

    let rec exec
        (partitionId: 'PartitionId)
        (context: Context) 
        (commandWrapper: ('State option) -> Result<'Events seq, Error>)
        (foldWrapper: ('State option) -> ('Events seq) -> 'State)
        :Async<Result<unit, Error>> =

        let execWithSnapshot
            (snapshotAndVersion: (Snapshot<'State> * KeyValueVersion) option)
            :Async<Result<unit, Error>> =
            async {
                //let (position, state, version)
                let events =
                    match snapshotAndVersion with
                    | Some (snapshot, version) ->
                        ResultModule.result {
                            let! events = commandWrapper (snapshot.State |> Some)
                            
                            let state = foldWrapper (snapshot.State |> Some)

                            return 
                                async {
                                    //do! kafkaTopic.BeginTransaction()
                                    

                                    let asd = 
                                        events
                                        |> Seq.fold
                                            (fun result event ->
                                                result
                                                |> Option.defaultValue (async { return (-1 |> Result.Ok) })
                                                |> Async.RunSynchronously
                                                |> ResultModule.bimap
                                                    (fun _ -> 
                                                        kafkaTopic.Add(partitionId, event) 
                                                    )
                                                    id
                                                |> Some
                                            )
                                            None

                                    for event in events do
                                        let! asd = kafkaTopic.Add(partitionId, event)
                                        ()
                                      
                                    config.SnapshotStore.Put
                                        partitionId
                                        state
                                        version
                                }
                        }

                    | None -> 
                        ResultModule.result {
                            let! events = commandWrapper None
                            return ()
                        }

                return () |> Result.Ok
            }

        let asd = command.Exec context 

        async {
            let! snapshotAndVersionResult = config.SnapshotStore.Get partitionId

            let asd =
                snapshotAndVersionResult
                |> ResultModule.bimap
                    execWithSnapshot
                    (fun x -> Sunergeo.Core.Todo.todo())

            return Sunergeo.Core.Todo.todo()
        }
    
    //member this.ReadFrom(partitionId: 'PartitionId, positionId: int): Async<Result<'Events seq, EventSourceError>> = 
    //    async {
    //        let! entries = kafkaTopic.ReadFrom(partitionId, positionId)

    //        return 
    //            entries 
    //            |> Seq.map (fun x -> x.Item) 
    //            |> Result.Ok
    //    }

    member this.Exec(context: Context, command: ICommandBase<'PartitionId>):Async<Result<unit, Error>> =
        async {
            let partitionId = 
                context
                |> command.GetId 

            return 
                ResultModule.bimap
                    (fun stateAndVersion ->
                        ()
                    )
                    (fun error ->
                        Sunergeo.Core.Todo.todo()
                    )
        }