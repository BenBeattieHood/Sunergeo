namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor

type MemoryEventSourcePartitionId = int
type MemoryEventSourcePollingActorConfig<'ProjectionId, 'Init, 'Events when 'ProjectionId : comparison> = {
    InstanceId: InstanceId
    Logger: Logger
    MemoryEventSource: Sunergeo.
    GetProjectionId: MemoryEventSourcePartitionId -> 'ProjectionId
}
type MemoryEventSourcePollingActor<'ProjectionId, 'Init, 'State, 'Events when 'ProjectionId : comparison>(config: MemoryEventSourcePollingActorConfig<'ProjectionId, 'Init, 'Events>, onEvent: ('ProjectionId * EventLogItem<'ProjectionId, 'Init, 'Events>) -> unit) as this =
    inherit ReceiveActor()

    let kvp 
        (keyAndValue: 'a * 'b)
        :System.Collections.Generic.KeyValuePair<'a, 'b> =
        let key, value = keyAndValue
        System.Collections.Generic.KeyValuePair(key, value)
        
    let onMemoryEventSourceMessage
        (message: Confluent.MemoryEventSource.Message)
        :unit =
        let projectionId = message.Partition |> config.GetProjectionId
        let events:EventLogItem<'ProjectionId, 'Init, 'Events> = Sunergeo.Core.Todo.todo()
        (projectionId, events) |> onEvent

    let topic = 
        config.InstanceId 
        |> Utils.toTopic<'State>
        
    let consumer = new Confluent.MemoryEventSource.Consumer(consumerConfiguration)

    do consumer.OnMessage.Add 
        (fun message ->
            if message.Topic = topic
            then 
                message |> onMemoryEventSourceMessage
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
    