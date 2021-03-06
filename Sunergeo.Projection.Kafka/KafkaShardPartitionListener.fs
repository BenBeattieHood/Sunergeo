﻿namespace Sunergeo.Projection.Kafka

open Sunergeo.Core
open Sunergeo.Logging
open Sunergeo.Kafka

open System
open Confluent.Kafka

type KafkaShardPartitionListenerConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison> = {
    Logger: Logger
    KafkaConsumerConfig: KafkaConsumerConfig
    ShardId: ShardId
    ShardPartitionPositionStore: Sunergeo.KeyValueStorage.IReadOnlyKeyValueStore<ShardPartition, ShardPartitionPosition, 'ShardPartitionStoreKeyValueVersion>
    OnRead: ShardPartition -> ShardPartitionPosition -> EventLogItem<'AggregateId, 'Init, 'Events> -> unit
    Deserialize: byte[] -> EventLogItem<'AggregateId, 'Init, 'Events>
}

type KafkaShardPartitionListener<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison>(config: KafkaShardPartitionListenerConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion>) =
    
    let kafkaConsumer = new Confluent.Kafka.Consumer(config.KafkaConsumerConfig |> KafkaConsumerConfig.toKafkaConfig)
    

    let assignKafkaPartitions
        (topicPartitions: TopicPartition seq)
        : unit =
        let retainTopicPartitionsAndPositions:(ShardPartition * ShardPartitionPosition option) seq =
            kafkaConsumer.Assignment
            |> kafkaConsumer.Position
            |> Seq.map
                (fun x ->
                    if x.Error.HasError
                    then
                        failwith "Cannot load offsets"
                    else
                        x.TopicPartition |> TopicPartition.toShardPartition,
                        x.Offset.Value |> Some
                )

        kafkaConsumer.Unassign()

        let shardPartitions =
            topicPartitions
            |> Seq.map TopicPartition.toShardPartition

        let shardPartitionsAndPositionsResult =
            shardPartitions
            |> Seq.map (fun a -> a |> config.ShardPartitionPositionStore.Get |> ResultModule.map (fun b -> a, b |> Option.map fst))
            |> ResultModule.ofSeq

        match shardPartitionsAndPositionsResult with
        | Result.Ok shardPartitionsAndPositions ->
            let kafkaTopicPartitionOffsets =
                shardPartitionsAndPositions
                |> Seq.append retainTopicPartitionsAndPositions
                |> Seq.map 
                    (fun (shardPartition, shardPartitionPosition) ->
                        TopicPartitionOffset(
                            (shardPartition |> TopicPartition.ofShardPartition),
                            (shardPartitionPosition |> Option.defaultValue (0 |> int64) |> Offset)
                            )
                    )
                |> List.ofSeq

            kafkaTopicPartitionOffsets
            |> kafkaConsumer.Assign

        | Result.Error error ->
            failwith "Cannot load offsets"

    let revokeKafkaPartitions
        (topicPartitions: TopicPartition seq)
        : unit =
        let removeTopicPartitions =
            topicPartitions
            |> Seq.map TopicPartition.toShardPartition
            |> Set.ofSeq
        let retainTopicPartitions =
            kafkaConsumer.Assignment
            |> Seq.map TopicPartition.toShardPartition
            |> Seq.filter removeTopicPartitions.Contains
            
        retainTopicPartitions 
        |> Seq.map TopicPartition.ofShardPartition 
        |> kafkaConsumer.Assign

    do kafkaConsumer.OnError.Add (fun error -> KafkaError.writeToLog config.Logger error)
    do kafkaConsumer.OnConsumeError.Add (fun message -> KafkaError.writeToLog config.Logger message.Error)
    do kafkaConsumer.OnPartitionsAssigned.Add assignKafkaPartitions
    do kafkaConsumer.OnPartitionsRevoked.Add revokeKafkaPartitions

    do kafkaConsumer.OnMessage.Add
        (fun message ->
            let shardPartition = message.TopicPartition |> TopicPartition.toShardPartition
            let shardPartitionPosition:ShardPartitionPosition = message.Offset.Value
            message.Value 
            |> config.Deserialize 
            |> config.OnRead shardPartition shardPartitionPosition
        )
    
    do kafkaConsumer.Subscribe [| config.ShardId |]
    let kafkaTopicMetadata = kafkaConsumer.GetMetadata(false).Topics |> Seq.find (fun topic -> topic.Topic = config.ShardId)
    do if kafkaTopicMetadata.Error.HasError 
        then KafkaError.writeToLog config.Logger kafkaTopicMetadata.Error
        else 
            let kafkaTopicPartitions =
                kafkaTopicMetadata.Partitions
                |> Seq.map
                    (fun partition ->
                        TopicPartition(
                            kafkaTopicMetadata.Topic,
                            partition.PartitionId
                            )
                    )
            assignKafkaPartitions kafkaTopicPartitions

    member this.Poll(duration: TimeSpan):unit =
        kafkaConsumer.Poll duration
    
    interface System.IDisposable with
        member this.Dispose() = 
            kafkaConsumer.Dispose()