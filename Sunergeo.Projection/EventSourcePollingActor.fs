namespace Sunergeo.Projection

open Sunergeo.Core
open Sunergeo.Logging

open System
open Akka.Actor

type EventSourcePollingActorConfig<'AggregateId, 'Init, 'Events, 'StateKeyValueVersion when 'AggregateId : comparison and 'StateKeyValueVersion : comparison> = {
    InstanceId: InstanceId
    Logger: Logger
    EventSource: Sunergeo.EventSourcing.Memory.IEventSource<'AggregateId, 'Init, 'Events>
    PollStateStore: Sunergeo.KeyValueStorage.IKeyValueStore<string, 'AggregateId * int, 'StateKeyValueVersion>
}
type EventSourcePollingActor<'AggregateId, 'Init, 'State, 'Events, 'StateKeyValueVersion when 'AggregateId : comparison and 'StateKeyValueVersion : comparison>(config: EventSourcePollingActorConfig<'AggregateId, 'Init, 'Events, 'StateKeyValueVersion>, onEvent: ('AggregateId * EventLogItem<'AggregateId, 'Init, 'Events>) -> unit) as this =
    inherit ReceiveActor()

    let kvp 
        (keyAndValue: 'a * 'b)
        :System.Collections.Generic.KeyValuePair<'a, 'b> =
        let key, value = keyAndValue
        System.Collections.Generic.KeyValuePair(key, value)
        
    let checkForEvents ():unit =
        let positions = config.EventSource.GetPositions()
        let pollState = topic |> config.PollStateStore.Get
        let aggregateId = message.Partition |> config.GetProjectionId
        let events:EventLogItem<'AggregateId, 'Init, 'Events> = Sunergeo.Core.Todo.todo()
        (aggregateId, events) |> onEvent

    let shardId = 
        config.InstanceId 
        |> Utils.toShardId<'State>
            
    let self = this.Self
    do this.Receive<unit>
        (fun message -> 
            checkForEvents()
            self.Tell(message)
        )

    override this.PreStart() =
        ReceiveActor.Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromMilliseconds(100.0), self, (), ActorRefs.Nobody)
        base.PreStart()
    