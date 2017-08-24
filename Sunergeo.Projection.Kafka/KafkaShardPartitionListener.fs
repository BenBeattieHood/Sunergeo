namespace Sunergeo.Projection.Kafka

open Sunergeo.Core
open Sunergeo.Logging
open Sunergeo.Kafka

open System
open Confluent.Kafka

type KafkaShardPartitionListenerConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison> = {
    Logger: Logger
    ConsumerConfig: KafkaConsumerConfig
    ShardId: ShardId
    ShardPartitionStore: Sunergeo.KeyValueStorage.IReadOnlyKeyValueStore<ShardPartition, ShardPartitionPosition, 'ShardPartitionStoreKeyValueVersion>
    OnRead: ShardPartition -> ShardPartitionPosition -> EventLogItem<'AggregateId, 'Init, 'Events> -> unit
    Deserialize: byte[] -> EventLogItem<'AggregateId, 'Init, 'Events>
}

type KafkaShardPartitionListener<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison>(config: KafkaShardPartitionListenerConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion>) =
    
    let kafkaConsumer = new Confluent.Kafka.Consumer(config.ConsumerConfig |> KafkaConsumerConfig.toKafkaConfig)

    do kafkaConsumer.OnError.Add
        (fun error ->
            KafkaError.writeToLog config.Logger error
        )

    do kafkaConsumer.OnConsumeError.Add
        (fun message ->
            KafkaError.writeToLog config.Logger message.Error
        )

    do kafkaConsumer.OnPartitionsAssigned.Add
        (fun topicPartitions ->
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
                |> Seq.map (fun a -> a |> config.ShardPartitionStore.Get |> ResultModule.map (fun b -> a, b |> Option.map fst))
                |> ResultModule.ofSeq

            match shardPartitionsAndPositionsResult with
            | Result.Ok shardPartitionsAndPositions ->
                shardPartitionsAndPositions
                |> Seq.append retainTopicPartitionsAndPositions
                |> Seq.map 
                    (fun (shardPartition, shardPartitionPosition) ->
                        TopicPartitionOffset(
                            (shardPartition |> TopicPartition.ofShardPartition),
                            (shardPartitionPosition |> Option.defaultValue (0 |> int64) |> Offset)
                            )
                    )
                |> kafkaConsumer.Assign
            | Result.Error error ->
                failwith "Cannot load offsets"
        )

    do kafkaConsumer.OnPartitionsRevoked.Add
        (fun topicPartitions ->
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
        )

    do kafkaConsumer.OnMessage.Add
        (fun message ->
            let shardPartition = message.TopicPartition |> TopicPartition.toShardPartition
            let shardPartitionPosition:ShardPartitionPosition = message.Offset.Value
            message.Value 
            |> config.Deserialize 
            |> config.OnRead shardPartition shardPartitionPosition
        )
    
    do kafkaConsumer.Subscribe [| config.ShardId |]

    member this.Poll(duration: TimeSpan):unit =
        kafkaConsumer.Poll duration
    
    interface System.IDisposable with
        member this.Dispose() = 
            kafkaConsumer.Dispose()