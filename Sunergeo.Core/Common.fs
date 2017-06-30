namespace Sunergeo.Core

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