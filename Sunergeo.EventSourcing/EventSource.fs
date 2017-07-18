namespace Sunergeo.EventSourcing

open System
open Sunergeo.Core
open Sunergeo.KeyValueStorage
open Sunergeo.EventSourcing.Storage

type Snapshot<'State> = {
    Position: int64
    State: 'State
}

type EventSourceConfig<'PartitionId, 'State, 'Events when 'PartitionId : comparison> = {
    InstanceId: InstanceId
    Create: Context -> 'PartitionId -> 'State
    Fold: 'State -> 'Events -> 'State
    SnapshotStore: Sunergeo.KeyValueStorage.KeyValueStore<'PartitionId, Snapshot<'State>>
    LogUri: Uri
    Logger: Sunergeo.Logging.Logger
}

type EventSource<'PartitionId, 'State, 'Events when 'PartitionId : comparison>(config: EventSourceConfig<'PartitionId, 'State, 'Events>) = 
    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>

    let asd
        (asyncResult: Result<Async<Result<'Ok, 'Error1>>, 'Error2>)
        (mapError1ToError2: 'Error1 -> 'Error2)
        :unit =
        ()

    let logConfig:LogConfig = {
        Topic = topic
        Uri = config.LogUri
    }

    let kafkaTopic = new LogTopic<'PartitionId, EventLogItem<'PartitionId, 'Events>>(logConfig)
    
    let append
        (partitionId: 'PartitionId)
        (getNewStateAndEvents: (Snapshot<'State> * int) option -> Result<'State * (EventLogItem<'PartitionId, 'Events> seq) * (int option), Error>)
        :Async<Result<unit, Error>> =
        
        async {
            let snapshotAndVersion = 
                partitionId 
                |> config.SnapshotStore.Get 

            let (newState, events, version) = 
                snapshotAndVersion 
                |> ResultModule.get 
                |> getNewStateAndEvents
                |> ResultModule.get
            
            let! transactionId = kafkaTopic.BeginTransaction()
            
            try
                let mutable position = 0 |> int64
                
                for event in events do
                    let! positionResult = kafkaTopic.Add(partitionId, event)
                    position <- (positionResult |> ResultModule.get)    
                    
                let snapshot = {
                    Snapshot.Position = position
                    Snapshot.State = newState
                }

                let snapshotPutResult =
                    match version with
                    | None ->
                        config.SnapshotStore.Create
                            partitionId
                            snapshot

                    | Some version ->
                        config.SnapshotStore.Put
                            partitionId
                            (snapshot, version)

                do snapshotPutResult |> ResultModule.get
                
                do! kafkaTopic.CommitTransaction()

                return () |> Result.Ok
            with
                //| :? ResultModule.ResultException<Sunergeo.EventSourcing.Storage.LogError> as resultException ->
                //    do! kafkaTopic.AbortTransaction()
                //    return 
                //        match resultException.Error with
                //        | Timeout ->
                //            Error. Result.Error

                | :? ResultModule.ResultException<Sunergeo.KeyValueStorage.WriteError> as resultException ->
                    do! kafkaTopic.AbortTransaction()

                    return 
                        match resultException.Error with
                        | Sunergeo.KeyValueStorage.WriteError.Timeout -> 
                            Sunergeo.Core.Todo.todo()

                        | Sunergeo.KeyValueStorage.WriteError.InvalidVersion -> 
                            Sunergeo.Core.Todo.todo()

                        | Sunergeo.KeyValueStorage.WriteError.Error error -> 
                            error 
                            |> Sunergeo.Core.Error.InvalidOp 
                            |> Result.Error

                | :? _ as ex ->
                    do! kafkaTopic.AbortTransaction()
                    return Sunergeo.Core.NotImplemented.NotImplemented() //This needs transaction support

        }
    
    member this.Create(context: Context) (partitionId: 'PartitionId) (f: CreateCommandExec<'Events>): Async<Result<unit, Error>> =
        let apply
            (newEvents: 'Events seq)
            =
            let newState = config.Create context partitionId

            let newEvents = seq {
                yield 
                    {
                        EventSourceInitItem.Id = partitionId
                        EventSourceInitItem.CreatedOn = context.Timestamp
                    }
                    |> EventLogItem.Init

                yield!
                    newEvents 
                    |> Seq.map EventLogItem.Event
            }

            newState, newEvents, None

        append partitionId
            (fun snapshotAndVersion ->
                match snapshotAndVersion with
                | Some snapshotAndVersion ->
                    (sprintf "Expected empty state, found %O" snapshotAndVersion)
                    |> Error.InvalidOp
                    |> Result.Error

                | None -> 
                    f context 
                    |> ResultModule.map apply
            )

    member this.Append(context: Context) (partitionId: 'PartitionId) (f: CommandExec<'State, 'Events>): Async<Result<unit, Error>> =
        let apply
            (snapshot: Snapshot<'State>)
            (newEvents: 'Events seq)
            (version: int)
            =
            let newState = 
                newEvents
                |> Seq.fold config.Fold snapshot.State

            newState, (newEvents |> Seq.map EventLogItem.Event), (version |> Some)

        append partitionId
            (fun snapshotAndVersion ->
                match snapshotAndVersion with
                | None -> 
                    "State, found None"
                    |> Error.InvalidOp
                    |> Result.Error
                    
                | Some (snapshot, version) ->
                    f context snapshot.State 
                    |> ResultModule.map (fun newEvents -> apply snapshot newEvents version)
            )
        
    interface System.IDisposable with
        member this.Dispose() =
            (kafkaTopic :> IDisposable).Dispose()