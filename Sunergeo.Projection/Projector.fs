namespace Sunergeo.Projection

open Sunergeo.Core

open System
open Akka.Actor

[<AbstractClass>]
type Projector<'PartitionId, 'Events when 'PartitionId : comparison>() as this =
    inherit ReceiveActor()
    do this.Receive<EventLogItem<'PartitionId, 'Events>>(fun message -> this.Process(message))
    abstract member Process : EventLogItem<'PartitionId, 'Events> -> unit
    override this.PostStop() =
        (this :> IDisposable).Dispose()
        base.PostStop()
    interface System.IDisposable with
        member this.Dispose() = ()
