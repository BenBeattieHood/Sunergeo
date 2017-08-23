namespace Sunergeo.Projection.Kafka

open Sunergeo.Core
open Sunergeo.Projection
open Sunergeo.Logging
open Sunergeo.Kafka

open System

type KafkaShardPartitionListenerConfig<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = {
    Logger: Logger
    ConsumerConfig: KafkaConsumerConfig
    ShardPartitions: Map<ShardPartition, ShardPartitionPosition>
    OnItemReceived: (EventLogItem<'AggregateId, 'Init, 'Events> -> unit) list
    Deserialize: byte[] -> EventLogItem<'AggregateId, 'Init, 'Events>
}

type KafkaShardPartitionListener<'AggregateId, 'Init, 'Events when 'AggregateId : comparison>(config: KafkaShardPartitionListenerConfig<'AggregateId, 'Init, 'Events>) =
    
    let kafkaConsumer = new Confluent.Kafka.Consumer(config.ConsumerConfig |> KafkaConsumerConfig.toKafkaConfig)
    
    do kafkaConsumer.OnMessage.Add
        (fun message ->
            if (config.ShardPartitions |> Map.containsKey ({ ShardPartition.ShardId = message.Topic; ShardPartition.ShardPartitionId = message.Partition }))
            then
                let eventLogItem = message.Value |> config.Deserialize
                for onItemReceived in config.OnItemReceived do
                    eventLogItem |> onItemReceived
            else
                sprintf "Received message for unexpected topic/partition %s:%i (listening on %s)" message.Topic message.Partition (config.ShardPartitions |> Map.toSeq |> Seq.map fst |> ProjectionUtils.getShardPartitionsName)
                |> config.Logger LogLevel.Error
        )

    let kafkaTopicPartitionPositions =
        config.ShardPartitions
        |> Map.toSeq
        |> Seq.map
            (fun (shardPartition, shardPartitionPosition) ->
                let topicPartition = Confluent.Kafka.TopicPartition(shardPartition.ShardId, shardPartition.ShardPartitionId)
                let offset = Confluent.Kafka.Offset(shardPartitionPosition)
                Confluent.Kafka.TopicPartitionOffset(tp = topicPartition, offset = offset)
            )
            
    do kafkaConsumer.Assign kafkaTopicPartitionPositions

    member this.Poll(duration: TimeSpan):unit =
        kafkaConsumer.Poll duration
    
    interface System.IDisposable with
        member this.Dispose() = 
            kafkaConsumer.Dispose()