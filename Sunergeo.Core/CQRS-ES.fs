namespace Sunergeo.Core

type IEvent = interface end

type ICommandBase<'Id when 'Id : comparison> =
    abstract GetId: Context -> 'Id
    
type ICreateCommand<'Id, 'Event when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> Result<'Event seq, Error>

type ICommand<'Id, 'Event, 'State when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> 'State -> Result<'Event seq, Error>
    
type IUnvalidatedCommand<'Id, 'Event when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> Result<'Event seq, Error>