namespace Sunergeo.EventSourcing

open System
open System.Text
open Sunergeo.Core
open Sunergeo.KeyValueStorage
open System.Diagnostics
open Kafunk
open Newtonsoft.Json

type Snapshot<'State> = {
    Position: int64
    State: 'State
}

type EventSourceConfig<'PartitionId, 'State, 'Events when 'PartitionId : comparison> = {
    InstanceId: InstanceId
    Fold: 'State -> 'Events -> 'State
    SnapshotStore: Sunergeo.KeyValueStorage.KeyValueStore<'PartitionId, Snapshot<'State>>
    LogUri: Uri
    Logger: Sunergeo.Logging.Logger
}

type LogEntry<'Item> = {
    Position: int   // in Kafka this is called the offset, and it is available after the item has been written to a partition
    Item: 'Item
}

type LogConfig = {
    Uri: Uri
    Topic: string
}

type LogError =
    Timeout
    | Error of string

type LogTransactionId = string

type LogTopic<'PartitionId, 'Item when 'PartitionId : comparison>(config: LogConfig) =
    let toAsync (a:'a): Async<'a> = async { return a }

    let kafkaConnection = Kafka.connHost (sprintf "%s:%i" config.Uri.Host config.Uri.Port)
    let producerMap : Map<'PartitionId, Producer> = Map.empty

    let consumerFunc (state:ConsumerState) (messageSet:ConsumerMessageSet): Async<unit> = 
        async {
                printfn "\nMESSAGE: member_id=%s topic=%s partition=%i" state.memberId messageSet.topic messageSet.partition
                printfn "%s" (System.Text.Encoding.ASCII.GetString messageSet.messageSet.messages.[0].message.value.Array)
        }


    let createProducer (partitionId: 'PartitionId) =
        let producerCfg =
              ProducerConfig.create (
                topic = config.Topic, 
                partition = Partitioner.konst (hash partitionId), 
                requiredAcks = RequiredAcks.Local)

        let producer =
            Producer.createAsync kafkaConnection producerCfg
            |> Async.RunSynchronously

        producer

    let addProducerToMap (partitionId: 'PartitionId) =
        let producer = createProducer partitionId
        producerMap.Add(partitionId, producer) |> ignore
        producer

    // https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
    member this.BeginTransaction(): Async<LogTransactionId> =
        async {
            return ("" : LogTransactionId)
        }

    member this.AbortTransaction(): Async<unit> =
        async {
            return Sunergeo.Core.NotImplemented.NotImplemented()
        }

    member this.CommitTransaction(): Async<unit> =
        async {
            return Sunergeo.Core.NotImplemented.NotImplemented()
        }

    member this.Add(partitionId: 'PartitionId, item: 'Item): Async<Result<int64, LogError>> =
        async {
            let producer = 
                producerMap
                |> Map.tryFind partitionId
                |> Option.defaultWith
                    (fun _ ->
                        addProducerToMap partitionId 
                    )           

            let jsonSerializer = JsonConvert.SerializeObject >> Encoding.ASCII.GetBytes
            let serializedItem = jsonSerializer item
            let producerMessage = ProducerMessage.ofBytes(serializedItem)

            let! result = Producer.produce producer producerMessage
            return (int64)result.offset |> Result.Ok
        }

    member this.ReadFrom(partitionId: 'PartitionId, positionId: int64): Async<Result<LogEntry<'Item> seq, LogError>> =
        async {         
            let topic = "turtle"
            let consumerGroup = "turtle-group"

            let consumerConfig = 
                ConsumerConfig.create (
                  groupId = consumerGroup, 
                  topic = topic, 
                  autoOffsetReset = AutoOffsetReset.StartFromTime Time.EarliestOffset,
                  fetchMaxBytes = 1000000,
                  fetchMinBytes = 1,
                  fetchMaxWaitMs = 1000,
                  fetchBufferSize = 1,
                  sessionTimeout = 30000,
                  heartbeatFrequency = 3,
                  checkCrc = true,
                  endOfTopicPollPolicy = RetryPolicy.constantMs 1000
                )

            let consumer =
              Consumer.create kafkaConnection consumerConfig

            // commit on every message set
            let mutable mutableSet:ConsumerMessageSet = new ConsumerMessageSet()

            let r = Consumer.consume consumer consumerFunc // |> Async.RunSynchronously

            //Consumer.consume consumer 
            //  (fun (state:ConsumerState) (messageSet:ConsumerMessageSet) -> async {
            //    printfn "member_id=%s topic=%s partition=%i" state.memberId messageSet.topic messageSet.partition
            //    //do! Consumer.commitOffsets consumer (ConsumerMessageSet.commitPartitionOffsets messageSet) 
            //    mutableSet <- messageSet
            //    })
            //|> Async.RunSynchronously
            
            let error = LogError.Error "Bah bow"
            let errorResult = Result.Error error

            return errorResult
        }
 
    interface System.IDisposable with
        member this.Dispose() =
            kafkaConnection.Close()
        
        

type EventSource<'PartitionId, 'State, 'Events when 'PartitionId : comparison>(config: EventSourceConfig<'PartitionId, 'State, 'Events>) = 
    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>

    let logConfig:LogConfig = {
        Topic = topic
        Uri = config.LogUri
    }

    let kafkaTopic = new LogTopic<'PartitionId, EventLogItem<'PartitionId, 'Events>>(logConfig)
    
    let rec exec
        (partitionId: 'PartitionId)
        (context: Context) 
        (fold: 'State -> 'Events -> 'State)
        (commandResult: CommandResult<'State, 'Events>)
        :Async<Result<unit, Error>> =
        
        async {
            //let partitionId = command.GetId context
            let snapshotAndVersionResult = config.SnapshotStore.Get partitionId

            let snapshotAndVersion =
                snapshotAndVersionResult
                |> ResultModule.get
                
            let newState, newEvents, version =
                match commandResult, snapshotAndVersion with
                | CommandResult.Create (newState, newEvents), None ->
                    let newEvents = seq {
                        yield 
                            {
                                EventSourceInitItem.Id = partitionId
                                EventSourceInitItem.CreatedOn = context.Timestamp
                            }
                            |> EventLogItem.Init

                        yield!
                            newEvents |> Seq.map EventLogItem.Event
                    }

                    newState, newEvents, None
                | CommandResult.Update newEvents, Some (snapshot, version) ->
                    let newState = 
                        newEvents
                        |> Seq.fold fold snapshot.State

                    newState, (newEvents |> Seq.map EventLogItem.Event), (version |> Some)

                | _ -> 
                    failwith "uhoh"
                //| CommandResult.Create _, Some state ->
                //    let asd = ("" |> Error.InvalidOp |> Result.Error)
                    
                    
            let! transactionId = kafkaTopic.BeginTransaction()
            
            try
                let mutable position:int64 option = None

                for event in newEvents do
                    let! positionResult = kafkaTopic.Add(partitionId, event) 
                    position <- (positionResult |> ResultModule.get |> Some)
                    
                match position with
                | Some position ->
                    let snapshot = {
                        Snapshot.Position = position
                        Snapshot.State = newState
                    }

                    let snapshotOverVersion = (snapshot, version |> Option.defaultValue 0)
                    Debug.WriteLine "Something"
                    let snapshotPutResult =
                        config.SnapshotStore.Put
                            partitionId
                            snapshotOverVersion

                    do snapshotPutResult |> ResultModule.get

                    do! kafkaTopic.CommitTransaction()

                | None ->
                    do! kafkaTopic.AbortTransaction()

                return () |> Result.Ok
            with
                | :? _ as ex ->
                    do! kafkaTopic.AbortTransaction()
                    return Sunergeo.Core.NotImplemented.NotImplemented() //This needs transaction support

                  
        }
    
    //member this.ReadFrom(partitionId: 'PartitionId, positionId: int): Async<Result<'Events seq, Error>> = 
    //    async {
    //        let! entries = kafkaTopic.ReadFrom(partitionId, positionId)

    //        return 
    //            entries 
    //            |> ResultModule.bimap
    //                (Seq.map (fun x -> x.Item))
    //                (fun x -> Sunergeo.Core.Todo.todo())
    //    }

    member this.Exec(partitionId: 'PartitionId, context: Context, commandResult: CommandResult<'State, 'Events>, fold: 'State -> 'Events-> 'State):Async<Result<unit, Error>> =
        commandResult
        |> exec partitionId context fold
        
    interface System.IDisposable with
        member this.Dispose() =
            (kafkaTopic :> IDisposable).Dispose()