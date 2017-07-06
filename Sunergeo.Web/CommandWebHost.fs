namespace Sunergeo.Web.Commands

open System
open Sunergeo.Core
open Sunergeo.Web

open Routing

//type LogConfig = {
//    LogName: string
//}

type RoutedCommand<'Command, 'State, 'Events> = RoutedType<'Command, CommandResult<'State, 'Events>>

type CommandHandler<'State, 'Events> = RoutedTypeRequestHandler<CommandResult<'State, 'Events>>

open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Sunergeo.Logging
open Microsoft.Extensions.DependencyInjection

type CommandWebHostStartupConfig<'State, 'Events> = {
    Logger: Sunergeo.Logging.Logger
    ContextProvider: HttpContext -> Context
    Handlers: CommandHandler<'State, 'Events> list
    OnHandle: CommandResult<'State, 'Events> -> unit
}

type CommandWebHostStartup<'State, 'Events> (config: CommandWebHostStartupConfig<'State, 'Events>) = 

    member x.Configure 
        (
            app: IApplicationBuilder, 
            hosting: IHostingEnvironment
        ): unit =

        let reqHandler (ctx: HttpContext) = 
            async {
                sprintf "Received %O" ctx.Request.Path
                |> config.Logger Sunergeo.Logging.LogLevel.Information

                let context = ctx |> config.ContextProvider

                let result =
                    config.Handlers
                    |> List.tryPick
                        (fun handler ->
                            handler context ctx.Request
                        )
                        
                result
                |> WebHost.processResultFor ctx.Response
                    (fun events ->
                        events |> config.OnHandle
                        None
                    )
                    (fun logLevel message -> 
                        config.Logger logLevel (sprintf "%O -> %s" ctx.Request.Path message)
                    )

            } |> Async.StartAsTask :> Task

        app.Run(RequestDelegate(reqHandler))

type CommandWebHostConfig<'State, 'Events> = {
    Logger: Sunergeo.Logging.Logger
    Commands: RoutedCommand<obj, 'State, 'Events> list
    OnHandle: CommandResult<'State, 'Events> -> unit
    BaseUri: Uri
}

module CommandWebHost =
    let toGeneralRoutedCommand<'Command, 'State, 'Events>
        (command: RoutedCommand<'Command, 'State, 'Events>)
        :RoutedCommand<obj, 'State, 'Events> =
        {
            RoutedCommand.PathAndQuery = command.PathAndQuery
            RoutedCommand.HttpMethod = command.HttpMethod
            RoutedCommand.Exec = 
                (fun (wrappedTarget: obj) (request: HttpRequest) ->
                    command.Exec (wrappedTarget :?> 'Command) request
                )
        }

    let create<'State, 'Events> (config: CommandWebHostConfig<'State, 'Events>): IWebHost =
        let handlers = 
            config.Commands
            |> List.map Routing.createHandler
            
        let startupConfig:CommandWebHostStartupConfig<'State, 'Events> =
            {
                Logger = config.Logger
                Handlers = handlers
                OnHandle = config.OnHandle
            }

        WebHostBuilder()
            .ConfigureServices(fun services -> services.AddSingleton<CommandWebHostStartupConfig<'State, 'Events>>(startupConfig) |> ignore)
            .UseKestrel()
            .UseStartup<CommandWebHostStartup<'State, 'Events>>()
            .UseUrls(config.BaseUri |> string)
            .Build()
      