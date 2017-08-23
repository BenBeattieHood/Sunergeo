namespace Sunergeo.Projection.Kafka

open Sunergeo.Core
open Sunergeo.Projection
open Sunergeo.Logging
open Sunergeo.Kafka

open System

type KafkaShardPartitionListenerConfig<'AggregateId, 'Metadata, 'Init, 'Events when 'AggregateId : comparison> = {
    Logger: Logger
    ConsumerConfig: KafkaConsumerConfig
    ShardPartitionOffsets: (ShardPartition * ShardPartitionOffset) list
    OnItemReceived: EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events> -> unit
    Deserialize: byte[] -> EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events>
}

type KafkaShardPartitionListener<'AggregateId, 'Metadata, 'Init, 'Events when 'AggregateId : comparison>(config: KafkaShardPartitionListenerConfig<'AggregateId, 'Metadata, 'Init, 'Events>) =
    
    let kafkaConsumer = new Confluent.Kafka.Consumer(config.ConsumerConfig |> KafkaConsumerConfig.toKafkaConfig)

    let shardPartitions =
        config.ShardPartitionOffsets
        |> List.map fst
        |> Set.ofList

    do kafkaConsumer.OnMessage.Add
        (fun message ->
            if (shardPartitions |> Set.contains ({ ShardPartition.ShardId = message.Topic; ShardPartition.ShardPartitionId = message.Partition }))
            then
                message.Value |> config.Deserialize |> config.OnItemReceived
            else
                sprintf "Received message for unexpected topic/partition %s:%i (listening on %s)" message.Topic message.Partition (shardPartitions |> ProjectionUtils.getShardPartitionsName)
                |> config.Logger LogLevel.Error
        )

    let kafkaTopicPartitionOffsets =
        config.ShardPartitionOffsets
        |> Seq.map
            (fun (shardPartition, shardPartitionOffset) ->
                let topicPartition = Confluent.Kafka.TopicPartition(shardPartition.ShardId, shardPartition.ShardPartitionId)
                let offset = Confluent.Kafka.Offset(shardPartitionOffset)
                Confluent.Kafka.TopicPartitionOffset(tp = topicPartition, offset = offset)
            )
            
    do kafkaConsumer.Assign kafkaTopicPartitionOffsets

    member this.Poll(duration: TimeSpan):unit =
        kafkaConsumer.Poll duration
    
    interface System.IDisposable with
        member this.Dispose() = 
            kafkaConsumer.Dispose()