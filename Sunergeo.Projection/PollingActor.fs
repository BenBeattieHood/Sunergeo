namespace Sunergeo.Projection.Kafka

open Sunergeo.Core
open Sunergeo.Logging
open Akka.Actor

open System

type PollingActorConfig<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = {
    OnRead: ShardPartition -> ShardPartitionPosition -> EventLogItem<'AggregateId, 'Init, 'Events> -> unit
    Poll: unit -> unit
}

type PollingActor<'AggregateId, 'Init, 'Events when 'AggregateId : comparison>(config: PollingActorConfig<'AggregateId, 'Init, 'Events>) as this =
    inherit ReceiveActor()
    
    let self = this.Self
    do this.Receive<unit>
        (fun _ -> 
            config.Poll()
            self.Tell(())
        )

    override this.PreStart() =
        ReceiveActor.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100.0), self, (), ActorRefs.Nobody)
        base.PreStart()