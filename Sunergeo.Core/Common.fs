namespace Sunergeo.Core

type InstanceId = string
type UserId = string

type Context = {
    UserId: UserId
    WorkingAsUserId: UserId
    Timestamp: NodaTime.Instant
}

type ErrorStatus =
      InvalidOp
    | PermissionDenied
    | Unknown

type Error = {
    Status: ErrorStatus
    Message: string
}
with
    static member InvalidOp message = 
        {
            Error.Status = ErrorStatus.InvalidOp
            Message = message
        }
    static member PermissionDenied message = 
        {
            Error.Status = ErrorStatus.PermissionDenied
            Message = message
        }

type AsyncResult<'Ok, 'Error> = Async<Result<'Ok, 'Error>>

module Todo =
    // Marker method for quickly stubbing out methods'results
    let todo ():'a = Unchecked.defaultof<'a>

module NotImplemented =
    // Marker method for quickly stubbing out methods'results
    let NotImplemented ():'a = Unchecked.defaultof<'a>