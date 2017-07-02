namespace Sunergeo.Web

open System
open Sunergeo.Core

type LogConfig = {
    LogName: string
}

type RequestHandler = 
    Microsoft.AspNetCore.Http.HttpRequest -> (Result<unit, Error> option)

type StartupConfig = {
    Logger: Sunergeo.Logging.Logger
    Handlers: RequestHandler list
}


open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Sunergeo.Logging
open Microsoft.Extensions.DependencyInjection;


type Startup (config: StartupConfig) = 

    member x.Configure 
        (
            app: IApplicationBuilder, 
            hosting: IHostingEnvironment
        ): unit =

        let reqHandler (ctx: HttpContext) = 
            async {
                sprintf "Received %O" ctx.Request.Path
                |> config.Logger Sunergeo.Logging.LogLevel.Information

                let result =
                    config.Handlers
                    |> List.tryPick
                        (fun handler ->
                            handler ctx.Request
                        )
                        
                match result with
                | Some (Result.Ok _) ->
                    ctx.Response.StatusCode <- StatusCodes.Status204NoContent
                    
                    sprintf "%O -> %i" ctx.Request.Path ctx.Response.StatusCode
                    |> config.Logger LogLevel.Information


                | Some (Result.Error error) ->
                    match error.Status with
                    | ErrorStatus.PermissionDenied ->
                        ctx.Response.StatusCode <- StatusCodes.Status401Unauthorized

                    | ErrorStatus.Unknown ->
                        ctx.Response.StatusCode <- StatusCodes.Status500InternalServerError
                        
                    
                    sprintf "%O -> %i" ctx.Request.Path ctx.Response.StatusCode
                    |> config.Logger LogLevel.Warning

                    do! ctx.Response.WriteAsync (error.Message) |> Async.AwaitTask


                | None ->
                    ctx.Response.StatusCode <- StatusCodes.Status404NotFound

                    sprintf "%O -> %i" ctx.Request.Path ctx.Response.StatusCode
                    |> config.Logger LogLevel.Error


            } |> Async.StartAsTask :> Task

        app.Run(RequestDelegate(reqHandler))
        
        
type WebHostRoutedCommand = {
    Path: string
    CommandType: Type
}

type WebHostConfig = {
    Logger: Sunergeo.Logging.Logger option
    Commands: WebHostRoutedCommand list
    BaseUri: Uri
}

module WebHost =
    open System.Text.RegularExpressions

    let pathVariableRegex = Regex(@"\{(.+?)\}", RegexOptions.Compiled)
    let pathToRegexString
        (path: string)
        :string =
        pathVariableRegex.Replace 
            (
                path,
                (fun x ->
                    let name = 
                        if x.Value = "_" 
                        then x.Index |> string
                        else x.Value
                    sprintf "(?<%s>:.+?)" name
                )
            )

    let createHandler 
        (command: WebHostRoutedCommand)
        :RequestHandler =
        let regexString = command.Path |> pathToRegexString
        let regex = Regex(regexString, RegexOptions.Compiled)

        let ctor = command.CommandType.GetConstructors().[0]      // assume a record type
        let ctorParams = ctor.GetParameters()

        (fun (request: Microsoft.AspNetCore.Http.HttpRequest) ->
            // TODO: add path handler
            None
        )

    let create (config: WebHostConfig): IWebHost =
        let handlers = 
            config.Commands
            |> List.map createHandler
            
        let startupConfig =
            {
                StartupConfig.Logger = config.Logger
                StartupConfig.Handlers = handlers
            }

        WebHostBuilder()
            .ConfigureServices(fun services -> services.AddSingleton<StartupConfig>(startupConfig) |> ignore)
            .UseKestrel()
            .UseStartup<Startup>()
            .UseUrls(config.BaseUri |> string)
            .Build()
      