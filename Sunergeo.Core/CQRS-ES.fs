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
    
type CreateCommandExec<'Events> = Context -> Result<'Events seq, Error>
type ICreateCommand<'Id, 'State, 'Events when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: CreateCommandExec<'Events>

type CommandExec<'State, 'Events> = Context -> 'State -> Result<'Events seq, Error>
type ICommand<'Id, 'State, 'Events when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: CommandExec<'State, 'Events>
    
    
type CommandResult<'State, 'Events> =
    Create of 'Events seq
    | Update of 'Events seq


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
