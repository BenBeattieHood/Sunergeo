namespace Sunergeo.Core

type IEvent = interface end

type ICommandBase =
    abstract GetId: Context -> string

type ICommand<'TEvent, 'TState> =
    inherit ICommandBase
    abstract Exec: Context -> 'TState -> Microsoft.FSharp.Core.Result<'TEvent seq, Error>
    
type IUnvalidatedCommand<'TEvent> =
    inherit ICommandBase
    abstract Exec: Context -> Microsoft.FSharp.Core.Result<'TEvent seq, Error>