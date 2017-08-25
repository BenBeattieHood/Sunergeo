namespace Sunergeo.Projection.Kafka

open Sunergeo.Core
open Sunergeo.Logging
open Sunergeo.Kafka
open Sunergeo.Projection

open System

type KafkaProjectionHostConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison> = {
    ProjectionHostId: string
    Logger: Logger
    ShardPartitionPositionStore: Sunergeo.KeyValueStorage.IKeyValueStore<ShardPartition, ShardPartitionPosition, 'ShardPartitionStoreKeyValueVersion>
    Projectors: Projector<'AggregateId, 'Init, 'Events> list
    ProjectorsPerShardPartition: int
    KafkaConsumerConfig: KafkaConsumerConfig
    ShardId: ShardId
    Deserialize: byte[] -> EventLogItem<'AggregateId, 'Init, 'Events>
}
type KafkaProjectionHost<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion when 'AggregateId : comparison and 'ShardPartitionStoreKeyValueVersion : comparison>(config: KafkaProjectionHostConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion>) = 


    let projectionHostConfig:ProjectionHostConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion> =
        {
            ProjectionHostConfig.ProjectionHostId = config.ProjectionHostId
            ProjectionHostConfig.Logger = config.Logger
            ProjectionHostConfig.ShardPartitionPositionStore = config.ShardPartitionPositionStore
            ProjectionHostConfig.Projectors = config.Projectors
            ProjectionHostConfig.ProjectorsPerShardPartition = config.ProjectorsPerShardPartition
        }
    let projectionHost = new ProjectionHost<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion>(projectionHostConfig)
    

    let kafkaShardPartitionListenerConfig:KafkaShardPartitionListenerConfig<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion> =
        {
            KafkaShardPartitionListenerConfig.Logger = config.Logger
            KafkaShardPartitionListenerConfig.KafkaConsumerConfig = config.KafkaConsumerConfig
            KafkaShardPartitionListenerConfig.ShardId = config.ShardId
            KafkaShardPartitionListenerConfig.ShardPartitionPositionStore = config.ShardPartitionPositionStore
            KafkaShardPartitionListenerConfig.OnRead = projectionHost.OnEvent
            KafkaShardPartitionListenerConfig.Deserialize = config.Deserialize
        }
    let kafkaShardPartitionListener = new KafkaShardPartitionListener<'AggregateId, 'Init, 'Events, 'ShardPartitionStoreKeyValueVersion>(kafkaShardPartitionListenerConfig)
    

    interface System.IDisposable with
        member this.Dispose() = 
            try
                (projectionHost :> System.IDisposable).Dispose()
            finally
                (kafkaShardPartitionListener :> System.IDisposable).Dispose()