﻿namespace Sunergeo.Core

type IEvent = interface end


type EventSourceInitItem<'Id when 'Id : comparison> = 
    {
        Id: 'Id
        CreatedOn: NodaTime.Instant
    }
    interface IEvent
    
type EventLogItem<'Id, 'Events when 'Id : comparison> = 
    Init of EventSourceInitItem<'Id>
    | Event of 'Events


type ICommandBase<'Id when 'Id : comparison> =
    abstract GetId: Context -> 'Id
    
type ICreateCommand<'Id, 'State, 'Event when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> Result<'State * ('Event seq), Error>

type ICommand<'Id, 'State, 'Event when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> 'State -> Result<'Event seq, Error>
    