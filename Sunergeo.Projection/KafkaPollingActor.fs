namespace Sunergeo.Projection

open Sunergeo.Core

open System
open Akka.Actor

type KafkaPollingActorConfig = {
    GroupId: string
    AutoCommitIntervalMs: int option
    StatisticsIntervalMs: int
    Servers: string
}
type KafkaPollingActor(config: KafkaPollingActorConfig, onEvent: IActorRef -> Confluent.Kafka.Message -> unit) as this =
    inherit ReceiveActor()

    let kvp 
        (keyAndValue: 'a * 'b)
        :System.Collections.Generic.KeyValuePair<'a, 'b> =
        let key, value = keyAndValue
        System.Collections.Generic.KeyValuePair(key, value)

    let consumerConfiguration =
        seq<string * obj> {
            yield "group.id", upcast config.GroupId

            match config.AutoCommitIntervalMs with
            | Some autoCommitIntervalMs ->
                yield "enable.auto.commit", upcast true
                yield "auto.commit.interval.ms", upcast autoCommitIntervalMs
            | None -> ()

            yield "statistics.interval.ms", upcast config.StatisticsIntervalMs

            yield "bootstrap.servers", upcast config.Servers

            yield "default.topic.config", upcast [ ("auto.offset.reset", "smallest") |> kvp ]
        }
        |> Seq.map kvp

    let consumer = new Confluent.Kafka.Consumer(consumerConfiguration)

    let self = this.Self

    do consumer.OnMessage.Add (onEvent self)
            
    do consumer.Subscribe([ "tuneup" ]);

    do this.Receive<unit>
        (fun message -> 
            consumer.Poll(TimeSpan.FromSeconds 5.0)
            self.Tell(message)
        )
    
    override this.PostStop() =
        consumer.Dispose()
        base.PostStop()
    