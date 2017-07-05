namespace Sunergeo.Core

type IEvent = interface end

type ICommandBase<'Id when 'Id : comparison> =
    abstract GetId: Context -> 'Id
    
type ICreateCommand<'Id, 'State, 'Event when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> Result<'State * ('Event seq), Error>

type ICommand<'Id, 'State, 'Event when 'Id : comparison> =
    inherit ICommandBase<'Id>
    abstract Exec: Context -> 'State -> Result<'Event seq, Error>