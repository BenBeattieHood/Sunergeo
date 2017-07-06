namespace Sunergeo.EventSourcing

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

    let conn = Kafka.connHost "localhost:9092"
    let producerMap : Map<'PartitionId, Producer> = Map.empty

    let consumerFunc (state:ConsumerState) (messageSet:ConsumerMessageSet): Async<unit> = 
        async {
                printfn "member_id=%s topic=%s partition=%i" state.memberId messageSet.topic messageSet.partition
                printfn "%s" (System.Text.Encoding.ASCII.GetString messageSet.messageSet.messages.[0].message.value.Array)
        }


    let createProducer (partitionId: 'PartitionId) =
        let producerCfg =
              ProducerConfig.create (
                topic = config.Topic, 
                partition = Partitioner.konst (hash partitionId), 
                requiredAcks = RequiredAcks.Local)

        let producer =
            Producer.createAsync conn producerCfg
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
            return Sunergeo.Core.Todo.todo()
        }

    member this.CommitTransaction(): Async<unit> =
        async {
            return Sunergeo.Core.Todo.todo()
        }

    member this.Add(partitionId: 'PartitionId, item: 'Item): Async<Result<int64, LogError>> =
        async {
            let producer = match producerMap.ContainsKey partitionId with
                            | true -> producerMap.[partitionId]
                            | false -> addProducerToMap partitionId            

            let jsonSerializer = JsonConvert.SerializeObject >> Encoding.ASCII.GetBytes
            let serializedItem = jsonSerializer item
            let producerMessage = ProducerMessage.ofBytes(serializedItem)

            let! result = Producer.produce producer producerMessage
            return (int64)result.offset |> Result.Ok
        }

    member this.ReadFrom(partitionId: 'PartitionId, positionId: int64): Async<Result<LogEntry<'Item> seq, LogError>> =
        async {


            let consumerOffsets =
              Consumer.fetchOffsets conn "turtle-group" [||]
              |> Async.RunSynchronously

            //consumer            
            let group = "turtle-group"
            let topic = "turtle"

            let consumerConfig = 
                ConsumerConfig.create (
                  groupId = group, 
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
              Consumer.create conn consumerConfig

            // commit on every message set
            let mutable mutableSet:ConsumerMessageSet = new ConsumerMessageSet()

            let r = Consumer.consume consumer consumerFunc // |> Async.RunSynchronously
            // printf "PPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPPP"

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

            //let result =
            //    eventSource
            //    |> Map.tryFind partitionId
            //    |> function
            //        | Some items ->
            //            items
            //            |> Seq.skip positionId
            //        | None ->
            //            upcast [] 

            //return result |> Result.Ok
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
        config.InstanceId 
        |> Utils.toTopic<'State>

    let logConfig:LogConfig = {
        Topic = topic
        Uri = config.LogUri
    }

    let kafkaTopic = LogTopic<'PartitionId, EventLogItem<'PartitionId, 'Events>>(logConfig)
    
    let rec exec
        (context: Context) 
        (fold: 'State -> 'Events -> 'State)
        (command: ICommandBase<'PartitionId>)
        :Async<Result<unit, Error>> =
        
        async {
            let partitionId = command.GetId context
            let snapshotAndVersionResult = config.SnapshotStore.Get partitionId

            let snapshotAndVersion =
                snapshotAndVersionResult
                |> ResultModule.get
                
            let newState, newEvents, version =
                match command, snapshotAndVersion with
                | :? ICreateCommand<'PartitionId, 'State, 'Events> as createCommand, None ->
                    let newState, newEvents = 
                        createCommand.Exec context 
                        |> ResultModule.get

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
                | :? ICommand<'PartitionId, 'State, 'Events> as command, Some (snapshot, version) ->
                    let newEvents = 
                        snapshot.State
                        |> command.Exec context
                        |> ResultModule.get

                    let newState = 
                        newEvents
                        |> Seq.fold fold snapshot.State

                    newState, (newEvents |> Seq.map EventLogItem.Event), (version |> Some)
                | _ ->
                    failwith "uhoh"
                    
                    
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
                    return Sunergeo.Core.Todo.todo()

                  
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

    member this.Exec(context: Context, command: ICommandBase<'PartitionId>, fold: 'State -> 'Events-> 'State):Async<Result<unit, Error>> =
        command
        |> exec context fold