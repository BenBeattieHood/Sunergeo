namespace Sunergeo.Core

type IEvent = interface end

type ICommandBase =
    abstract GetId: Context -> string

type ICommand<'Event, 'State> =
    inherit ICommandBase
    abstract Exec: Context -> 'State -> Microsoft.FSharp.Core.Result<'Event seq, Error>
    
type IUnvalidatedCommand<'Event> =
    inherit ICommandBase
    abstract Exec: Context -> Microsoft.FSharp.Core.Result<'Event seq, Error>