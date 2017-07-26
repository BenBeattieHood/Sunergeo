﻿namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor

type KafkaPartitionId = int
type KafkaPollingActorConfig<'ProjectionId, 'Init, 'Events when 'ProjectionId : comparison> = {
    InstanceId: InstanceId
    Logger: Logger
    AutoCommitIntervalMs: int option
    StatisticsIntervalMs: int
    Servers: Uri[]
    GetProjectionId: KafkaPartitionId -> 'ProjectionId
}
type KafkaPollingActor<'ProjectionId, 'Init, 'State, 'Events when 'ProjectionId : comparison>(config: KafkaPollingActorConfig<'ProjectionId, 'Init, 'Events>, onEvent: ('ProjectionId * EventLogItem<'ProjectionId, 'Init, 'Events>) -> unit) as this =
    inherit ReceiveActor()

    let kvp 
        (keyAndValue: 'a * 'b)
        :System.Collections.Generic.KeyValuePair<'a, 'b> =
        let key, value = keyAndValue
        System.Collections.Generic.KeyValuePair(key, value)
        
    let onKafkaMessage
        (message: Confluent.Kafka.Message)
        :unit =
        let projectionId = message.Partition |> config.GetProjectionId
        let events:EventLogItem<'ProjectionId, 'Init, 'Events> = Sunergeo.Core.Todo.todo()
        (projectionId, events) |> onEvent

    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>

    let consumerConfiguration =
        seq<string * obj> {
            yield "group.id", upcast (topic + "-group")

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

    do consumer.OnMessage.Add 
        (fun message ->
            if message.Topic = topic
            then 
                message |> onKafkaMessage
            else
                sprintf "Received message for unexpected topic %s (listening on %s)" message.Topic topic
                |> config.Logger LogLevel.Error
        )
            
    let self = this.Self
    do consumer.Subscribe([ "tuneup" ]);
    do this.Receive<unit>
        (fun message -> 
            consumer.Poll(TimeSpan.FromSeconds 5.0)
            self.Tell(message)
        )

    override this.PreStart() =
        ReceiveActor.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100.0), self, (), ActorRefs.Nobody)
        base.PreStart()
    
    override this.PostStop() =
        consumer.Dispose()
        base.PostStop()
    