namespace Sunergeo.EventSourcing.Kafka

open Sunergeo.EventSourcing.Storage

open System
open System.Text
open Sunergeo.Core
open Sunergeo.Kafka
open ResultModule

type KafkaLogConfig<'AggregateId, 'Item when 'AggregateId : comparison> = {
    ProducerConfig: Sunergeo.Kafka.KafkaProducerConfig
    Topic: string
    Logger: Sunergeo.Logging.Logger
    SerializeAggregateId: 'AggregateId -> byte[]
    SerializeItem: 'Item -> byte[]
}
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module KafkaLogConfig =
    let createDefaultProducerConfig
        (clientId: string)
        (hosts: Sunergeo.Kafka.KafkaHost list)
        :Sunergeo.Kafka.KafkaProducerConfig =
        {
            Sunergeo.Kafka.KafkaProducerConfig.Default with
                BootstrapHosts = hosts
                ClientId = clientId
                Partitioner = null
                EnableIdempotence = true    // custom (default false)
                InflightRequestsPerConnectionMax = 1    // custom (default 5)
                TransactionalId = clientId
        }


type LogError =
    Timeout
    | Error of string

type LogTransactionId = string

type KafkaLogTopic<'AggregateId, 'Item when 'AggregateId : comparison>(config: KafkaLogConfig<'AggregateId, 'Item>) =

    let producer = new Confluent.Kafka.Producer(config.ProducerConfig |> Sunergeo.Kafka.KafkaProducerConfig.toKafkaConfig)
        
    member this.BeginTransaction(): Async<LogTransactionId> =
        // see https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
        async {
            return Sunergeo.Core.Todo.todo()
        }

    member this.AbortTransaction(): Async<unit> =
        // see https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
        async {
            return Sunergeo.Core.Todo.todo()
        }

    member this.CommitTransaction(): Async<unit> =
        // see https://www.confluent.io/blog/exactly-once-semantics-are-possible-heres-how-apache-kafka-does-it/
        async {
            return Sunergeo.Core.Todo.todo()
        }

    member this.Add(aggregateId: 'AggregateId, item: 'Item): Async<Result<ShardPartition * ShardPartitionPosition, LogError>> =
        async {
            let serializedAggregateId = aggregateId |> config.SerializeAggregateId
            let serializedItem = item |> config.SerializeItem
            
            let! result =
                producer.ProduceAsync(
                    config.Topic,
                    serializedAggregateId,
                    serializedItem
                    )
                |> Async.AwaitTask

            return (result.TopicPartition |> TopicPartition.toShardPartition, result.Offset.Value) |> Result.Ok
        }

    //member this.ReadFrom(aggregateId: 'AggregateId, positionId: int64): Async<Result<LogEntry<'Item> seq, LogError>> =

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
            producer.Dispose()