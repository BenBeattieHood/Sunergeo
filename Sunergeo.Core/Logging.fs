namespace Sunergeo.Logging

type LogLevel =
    Error
    | Information
    | Warning

type Logger = LogLevel -> string -> unit