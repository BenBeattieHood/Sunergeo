namespace Sunergeo.Core

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
    
type CreateCommandResult<'State, 'Events> = Result<'State * 'Events seq, Error>
type CreateCommandExec<'State, 'Events> = Context -> CreateCommandResult<'State, 'Events>
type ICreateCommand<'Id, 'State, 'Events when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> CreateCommandResult<'State, 'Events>
    
type UpdateCommandResult<'Events> = Result<'Events seq, Error>
type UpdateCommandExec<'State, 'Events> = Context -> 'State -> UpdateCommandResult<'Events>
type IUpdateCommand<'Id, 'State, 'Events when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> 'State -> UpdateCommandResult<'Events>
    


type IQuery<'Id, 'ReadStore, 'Result when 'Id : comparison> =
    abstract GetId: Context -> 'Id
    abstract Exec: Context -> 'ReadStore -> Result<'Result, Error>


module Utils =    
    let toTopic<'State>
        (instanceId: InstanceId)
        :string = 
        sprintf "%s-%s" 
            (typeof<'State>.Name)
            (instanceId |> string)
