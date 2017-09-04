namespace Sunergeo.Kafka

type KafkaHost = {
    Host: string
    Port: int
}
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module KafkaHost =
    let toString
        (x: KafkaHost)
        : string =
        sprintf "%s:%i" x.Host x.Port


type KafkaCompression =
| None
| Gzip
| Snappy
| Lz4
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module KafkaCompression =
    let toString =
        function
        | KafkaCompression.None -> "none"
        | Gzip -> "gzip"
        | Snappy -> "snappy"
        | Lz4 -> "lz4"

type SslStore = {
    Location: string
    Password: string option
}


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TopicPartition =
    let toShardPartition
        (topicPartition: Confluent.Kafka.TopicPartition)
        : Sunergeo.Core.ShardPartition =
        {
            ShardId = topicPartition.Topic
            ShardPartitionId = topicPartition.Partition
        } : Sunergeo.Core.ShardPartition

    let ofShardPartition
        (shardPartition: Sunergeo.Core.ShardPartition)
        : Confluent.Kafka.TopicPartition =
        Confluent.Kafka.TopicPartition(
            topic = shardPartition.ShardId,
            partition = shardPartition.ShardPartitionId
            )