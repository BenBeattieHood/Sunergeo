namespace Sunergeo.Core

type IEvent = interface end


type EventSourceInitItem<'Id, 'Init when 'Id : comparison> = 
    {
        Id: 'Id
        CreatedOn: NodaTime.Instant
        Init: 'Init
    }
    interface IEvent
    
type EventLogItem<'Id, 'Init, 'Events when 'Id : comparison> = 
    Init of EventSourceInitItem<'Id, 'Init>
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
    


type IQuery<'ReadStore, 'Result> =
    abstract Exec: Context -> 'ReadStore -> Async<Result<'Result, Error>>


type ShardId = string

module Utils =    
    let toShardId<'State>
        (instanceId: InstanceId)
        :ShardId = 
        sprintf "%s-%s" 
            (typeof<'State>.Name)
            (instanceId |> string)
