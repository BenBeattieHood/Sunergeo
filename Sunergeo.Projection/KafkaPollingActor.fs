namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor

type KafkaPartitionId = int
type KafkaPollingActorConfig<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = {
    InstanceId: InstanceId
    Logger: Logger
    AutoCommitIntervalMs: int option
    StatisticsIntervalMs: int
    Servers: Uri[]
    GetProjectionId: KafkaPartitionId -> 'AggregateId
}
type KafkaPollingActor<'AggregateId, 'Init, 'State, 'Events when 'AggregateId : comparison>(config: KafkaPollingActorConfig<'AggregateId, 'Init, 'Events>, onEvent: ('AggregateId * EventLogItem<'AggregateId, 'Init, 'Events>) -> unit) as this =
    inherit ReceiveActor()

    let kvp 
        (keyAndValue: 'a * 'b)
        :System.Collections.Generic.KeyValuePair<'a, 'b> =
        let key, value = keyAndValue
        System.Collections.Generic.KeyValuePair(key, value)
        
    let onKafkaMessage
        (message: Confluent.Kafka.Message)
        :unit =
        let aggregateId = message.Partition |> config.GetProjectionId
        let events:EventLogItem<'AggregateId, 'Init, 'Events> = Sunergeo.Core.Todo.todo()
        (aggregateId, events) |> onEvent

    let shardId = 
        config.InstanceId 
        |> Utils.toShardId<'State>

    let consumerConfiguration =
        seq<string * obj> {
            yield "group.id", upcast (shardId + "-consumergroup")

            match config.AutoCommitIntervalMs with
            | Some autoCommitIntervalMs ->
                yield "enable.auto.commit", upcast true
                yield "auto.commit.interval.ms", upcast autoCommitIntervalMs
            | None -> ()

            yield "statistics.interval.ms", upcast config.StatisticsIntervalMs

            yield "bootstrap.servers", upcast (config.Servers |> Array.map (fun uri -> sprintf "%s:%i" uri.Host uri.Port))

            yield "default.topic.config", upcast [ ("auto.offset.reset", "smallest") |> kvp ]
        }
        |> Seq.map kvp
        
    let consumer = new Confluent.Kafka.Consumer(consumerConfiguration)
    consumer.Assign(Confluent.Kafka.off

    do consumer.OnMessage.Add 
        (fun message ->
            if message.Topic = shardId
            then 
                message |> onKafkaMessage
            else
                sprintf "Received message for unexpected topic %s (listening on %s)" message.Topic shardId
                |> config.Logger LogLevel.Error
        )
            
    let self = this.Self
    do consumer.Subscribe([ shardId ]);
    do this.Receive<unit>
        (fun _ -> 
            consumer.Poll(TimeSpan.FromSeconds 5.0)
            self.Tell(())
        )

    override this.PreStart() =
        ReceiveActor.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100.0), self, (), ActorRefs.Nobody)
        base.PreStart()
    
    override this.PostStop() =
        consumer.Dispose()
        base.PostStop()
    