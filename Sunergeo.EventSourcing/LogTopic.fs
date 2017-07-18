namespace Sunergeo.EventSourcing.Storage

open System
open System.Text
open Sunergeo.Core
open Kafunk
open Newtonsoft.Json

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

    member this.BeginTransaction(): Async<LogTransactionId> =
        // see https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
        async {
            return ("" : LogTransactionId)
        }

    member this.AbortTransaction(): Async<unit> =
        // see https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
        async {
            return Sunergeo.Core.NotImplemented.NotImplemented()
        }

    member this.CommitTransaction(): Async<unit> =
        // see https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
        async {
            return Sunergeo.Core.NotImplemented.NotImplemented()
        }

    member this.Add(partitionId: 'PartitionId, item: 'Item): Async<Result<int64, LogError>> =
        async {
            let producer = 
                lock producerMap
                    (fun _ ->
                        producerMap
                        |> Map.tryFind partitionId
                        |> Option.defaultWith
                            (fun _ -> 
                                let producer = 
                                    partitionId 
                                    |> createProducer

                                producerMap.Add(partitionId, producer) 
                                |> ignore

                                producer
                            )           
                    )

            let jsonSerializer = JsonConvert.SerializeObject >> Encoding.ASCII.GetBytes
            let serializedItem = jsonSerializer item
            let producerMessage = ProducerMessage.ofBytes(serializedItem)

            let! result = Producer.produce producer producerMessage
            return (int64)result.offset |> Result.Ok
        }

    //member this.ReadFrom(partitionId: 'PartitionId, positionId: int64): Async<Result<LogEntry<'Item> seq, LogError>> =

    //    let consumerFunc (state:ConsumerState) (messageSet:ConsumerMessageSet): Async<unit> = 
    //        async {
    //                printfn "\nMESSAGE: member_id=%s topic=%s partition=%i" state.memberId messageSet.topic messageSet.partition
    //                printfn "%s" (System.Text.Encoding.ASCII.GetString messageSet.messageSet.messages.[0].message.value.Array)
    //        }
    //    async {         
    //        let topic = "turtle"
    //        let consumerGroup = "turtle-group"

    //        let consumerConfig = 
    //            ConsumerConfig.create (
    //              groupId = consumerGroup, 
    //              topic = topic, 
    //              autoOffsetReset = AutoOffsetReset.StartFromTime Time.EarliestOffset,
    //              fetchMaxBytes = 1000000,
    //              fetchMinBytes = 1,
    //              fetchMaxWaitMs = 1000,
    //              fetchBufferSize = 1,
    //              sessionTimeout = 30000,
    //              heartbeatFrequency = 3,
    //              checkCrc = true,
    //              endOfTopicPollPolicy = RetryPolicy.constantMs 1000
    //            )

    //        let consumer =
    //          Consumer.create kafkaConnection consumerConfig

    //        // commit on every message set
    //        let mutable mutableSet:ConsumerMessageSet = new ConsumerMessageSet()

    //        let r = Consumer.consume consumer consumerFunc // |> Async.RunSynchronously

    //        //Consumer.consume consumer 
    //        //  (fun (state:ConsumerState) (messageSet:ConsumerMessageSet) -> async {
    //        //    printfn "member_id=%s topic=%s partition=%i" state.memberId messageSet.topic messageSet.partition
    //        //    //do! Consumer.commitOffsets consumer (ConsumerMessageSet.commitPartitionOffsets messageSet) 
    //        //    mutableSet <- messageSet
    //        //    })
    //        //|> Async.RunSynchronously
            
    //        let error = LogError.Error "Bah bow"
    //        let errorResult = Result.Error error

    //        return errorResult
    //    }
 
    interface System.IDisposable with
        member this.Dispose() =
            kafkaConnection.Close()