module WebHost

open Sunergeo.Core
open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Sunergeo.Logging
open Microsoft.Extensions.DependencyInjection

let toJsonBytes
    (o: obj)
    : byte[] =
    let jsonResult = ""
    jsonResult |> System.Text.Encoding.UTF8.GetBytes
    
let processResultFor<'Result>
    (response: HttpResponse)
    (okHandler: 'Result -> ((obj * int) option))
    (writeToLog: LogLevel -> string -> unit)
    (result: Result<'Result, Error> option)
    : unit = 
                        
    match result with
    | Some (Result.Ok value) ->
        match value |> okHandler with
        | None ->
            response.StatusCode <- StatusCodes.Status204NoContent
        | Some (result, statusCode) ->
            response.StatusCode <- StatusCodes.Status200OK
            response.ContentType <- "application/json"
            let data = result |> toJsonBytes
            response.Body.Write(data, 0, data.Length)

        sprintf "%i" response.StatusCode
        |> writeToLog LogLevel.Information
                    

    | Some (Result.Error error) ->
        match error.Status with
        | ErrorStatus.InvalidOp ->
            response.StatusCode <- StatusCodes.Status400BadRequest
                        
        | ErrorStatus.PermissionDenied ->
            response.StatusCode <- StatusCodes.Status401Unauthorized

        | ErrorStatus.Unknown ->
            response.StatusCode <- StatusCodes.Status500InternalServerError
                        
                    
        sprintf "%i" response.StatusCode
        |> writeToLog LogLevel.Warning

        let error = error.Message |> toJsonBytes
        response.Body.Write(error, 0, error.Length)


    | None ->
        response.StatusCode <- StatusCodes.Status404NotFound

        sprintf "%i" response.StatusCode
        |> writeToLog LogLevel.Error
