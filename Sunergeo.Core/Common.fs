namespace Sunergeo.Core

type InstanceId = string
type UserId = string

type Context = {
    UserId: UserId
    WorkingAsUserId: UserId
    Timestamp: NodaTime.Instant
}

type ErrorStatus =
      PermissionDenied
    | Unknown

type Error = {
    Status: ErrorStatus
    Message: string
}

type AsyncResult<'Ok, 'Error> = Async<Result<'Ok, 'Error>>

module Todo =
    // Marker method for quickly stubbing out methods'results
    let todo ():'a = Unchecked.defaultof<'a>
