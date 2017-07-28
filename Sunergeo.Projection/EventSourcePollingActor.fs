namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor

type EventSourcePartitionId = int
type EventSourcePollingActorConfig<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = {
    InstanceId: InstanceId
    Logger: Logger
    EventSource: Sunergeo.EventSourcing.Memory.IEventSource<'AggregateId, 'Init, 'Events>
    GetProjectionId: EventSourcePartitionId -> 'AggregateId
}
type EventSourcePollingActor<'AggregateId, 'Init, 'State, 'Events when 'AggregateId : comparison>(config: EventSourcePollingActorConfig<'AggregateId, 'Init, 'Events>, onEvent: ('AggregateId * EventLogItem<'AggregateId, 'Init, 'Events>) -> unit) as this =
    inherit ReceiveActor()

    let kvp 
        (keyAndValue: 'a * 'b)
        :System.Collections.Generic.KeyValuePair<'a, 'b> =
        let key, value = keyAndValue
        System.Collections.Generic.KeyValuePair(key, value)
        
    let onEventSourceMessage
        (message: Confluent.EventSource.Message)
        :unit =
        let aggregateId = message.Partition |> config.GetProjectionId
        let events:EventLogItem<'AggregateId, 'Init, 'Events> = Sunergeo.Core.Todo.todo()
        (aggregateId, events) |> onEvent

    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>
        
    let consumer = new Confluent.EventSource.Consumer(consumerConfiguration)

    do consumer.OnMessage.Add 
        (fun message ->
            if message.Topic = topic
            then 
                message |> onEventSourceMessage
            else
                sprintf "Received message for unexpected topic %s (listening on %s)" message.Topic topic
                |> config.Logger LogLevel.Error
        )
            
    let self = this.Self
    do this.Receive<unit>
        (fun message -> 
            consumer.Poll(TimeSpan.FromSeconds 5.0)
            self.Tell(message)
        )

    override this.PreStart() =
        ReceiveActor.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100.0), self, (), ActorRefs.Nobody)
        base.PreStart()
    