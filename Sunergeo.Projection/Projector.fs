namespace Sunergeo.Projection

open Sunergeo.Core

open System
open Akka.Actor

[<AbstractClass>]
type Projector<'PartitionId, 'Init, 'Events when 'PartitionId : comparison>() as this =
    inherit ReceiveActor()
    do this.Receive<EventLogItem<'PartitionId, 'Init, 'Events>>(fun message -> this.Process(message))
    abstract member Process : EventLogItem<'PartitionId, 'Init, 'Events> -> unit
    override this.PostStop() =
        (this :> IDisposable).Dispose()
        base.PostStop()
    interface System.IDisposable with
        member this.Dispose() = ()
