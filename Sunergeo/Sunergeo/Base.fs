namespace Sunergeo

type UserId = string

type Context = {
    userId: UserId
    workingAsUserId: UserId
    timestamp: NodaTime.Instant
}

type ErrorStatus =
      PermissionDenied
    | Unknown


type Error = {
    status: ErrorStatus
    message: string
}