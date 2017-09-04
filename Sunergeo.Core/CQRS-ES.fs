namespace Sunergeo.Core

type IEvent = interface end

type EventLogItemData<'Init, 'Events> = 
    Init of 'Init
    | Event of 'Events
    //| End of NodaTime.Instant
    
type EventLogItemMetadata<'AggregateId when 'AggregateId : comparison> = {
    InstanceId: InstanceId
    AggregateId: 'AggregateId
    CorrelationId: CorrelationId
    FromCorrelationId: CorrelationId option
    Timestamp: NodaTime.Instant
}
type EventLogItem<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = {
    Metadata: EventLogItemMetadata<'AggregateId>
    Data: EventLogItemData<'Init, 'Events>
}



type ICommandBase<'AggregateId when 'AggregateId : comparison> =
    abstract GetId: Context -> 'AggregateId
    
type CreateCommandResult<'Init, 'Events> = Result<'Init * 'Events seq, Error>
type CreateCommandExec<'Init, 'Events> = Context -> CreateCommandResult<'Init, 'Events>
type ICreateCommand<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> =
    inherit ICommandBase<'AggregateId>
    abstract Exec: Context -> CreateCommandResult<'Init, 'Events>
    
type UpdateCommandResult<'Events> = Result<'Events seq, Error>
type UpdateCommandExec<'State, 'Events> = Context -> 'State -> UpdateCommandResult<'Events>
type IUpdateCommand<'AggregateId, 'State, 'Events when 'AggregateId : comparison> =
    inherit ICommandBase<'AggregateId>
    abstract Exec: Context -> 'State -> UpdateCommandResult<'Events>
    
//type DeleteCommandResult = Result<unit, Error>
//type DeleteCommandExec<'State> = Context -> 'State -> DeleteCommandResult
//type IDeleteCommand<'AggregateId, 'State when 'AggregateId : comparison> =
//    inherit ICommandBase<'AggregateId>
//    abstract Exec: Context -> 'State -> DeleteCommandResult
    


type IQuery<'ReadStore, 'Result> =
    abstract Exec: Context -> 'ReadStore -> Async<Result<'Result, Error>>


type ShardId = string
type ShardPartitionId = int
type ShardPartitionPosition = int64
type ShardPartition = {
    ShardId: ShardId
    ShardPartitionId: ShardPartitionId
}


type Snapshot<'State> = {
    ShardPartition: ShardPartition
    ShardPartitionPosition: ShardPartitionPosition
    State: 'State
}
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Snapshot =
    let state
        (snapshot: Snapshot<'State>)
        :'State =
        snapshot.State

module Utils =    
    let toShardId<'State>
        (instanceId: InstanceId)
        :ShardId = 
        sprintf "%s-%s" 
            (typeof<'State>.Name)
            (instanceId |> string)

    let createCorrelationId
        ()
        :CorrelationId =
        System.Guid.NewGuid().ToByteArray() |> CorrelationId
