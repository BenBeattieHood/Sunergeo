namespace Sunergeo.Projection

open Sunergeo.Core

open System
open Akka.Actor

[<AbstractClass>]
type Projector<'AggregateId, 'Init, 'Events when 'AggregateId : comparison>() as this =
    inherit ReceiveActor()
    do this.Receive<EventLogItem<'AggregateId, 'Init, 'Events>>(fun message -> this.Process(message))
    abstract member Process : EventLogItem<'AggregateId, 'Init, 'Events> -> unit
    override this.PostStop() =
        (this :> IDisposable).Dispose()
        base.PostStop()
    interface System.IDisposable with
        member this.Dispose() = ()
