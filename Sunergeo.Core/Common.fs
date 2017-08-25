namespace Sunergeo.Core

type InstanceId = int
type CorrelationId = System.Guid
type UserId = string

type Context = {
    InstanceId: InstanceId
    UserId: UserId
    WorkingAsUserId: UserId
    FromCorrelationId: CorrelationId option
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