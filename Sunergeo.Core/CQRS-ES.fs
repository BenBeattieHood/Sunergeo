﻿namespace Sunergeo.Core

type IEvent = interface end

type EventLogItemData<'AggregateId, 'Init, 'Events when 'AggregateId : comparison> = 
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
type EventLogItem<'AggregateId, 'Metadata, 'Init, 'Events when 'AggregateId : comparison> = {
    Metadata: EventLogItemMetadata<'AggregateId>
    Data: EventLogItemData<'AggregateId, 'Init, 'Events>
}



type ICommandBase<'AggregateId when 'AggregateId : comparison> =
    abstract GetId: Context -> 'AggregateId
    
type CreateCommandResult<'State, 'Events> = Result<'State * 'Events seq, Error>
type CreateCommandExec<'State, 'Events> = Context -> CreateCommandResult<'State, 'Events>
type ICreateCommand<'AggregateId, 'State, 'Events when 'AggregateId : comparison> =
    inherit ICommandBase<'AggregateId>
    abstract Exec: Context -> CreateCommandResult<'State, 'Events>
    
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
type ShardPartitionOffset = int64
type ShardPartition = {
    ShardId: ShardId
    ShardPartitionId: ShardPartitionId
}

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
