namespace Sunergeo.Web.Commands

open System
open Sunergeo.Core
open Sunergeo.Web

open Routing

//type LogConfig = {
//    LogName: string
//}

type RoutedCommand<'Command, 'Events> = RoutedType<'Command, 'Events seq>

type CommandHandler<'Events> = RoutedTypeRequestHandler<'Events seq>

type CommandWebHostStartupConfig<'Events> = {
    Logger: Sunergeo.Logging.Logger
    Handlers: CommandHandler<'Events> list
    OnHandle: (('Events seq) -> unit)
}

open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Sunergeo.Logging
open Microsoft.Extensions.DependencyInjection


type CommandWebHostStartup<'Events> (config: CommandWebHostStartupConfig<'Events>) = 

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

type CommandWebHostConfig<'Events> = {
    Logger: Sunergeo.Logging.Logger
    Commands: RoutedCommand<obj, 'Events> list
    OnHandle: (('Events seq) -> unit)
    BaseUri: Uri
}

module CommandWebHost =
    let toGeneralRoutedCommand<'Command, 'Events>
        (command: RoutedCommand<'Command, 'Events>)
        :RoutedCommand<obj, 'Events> =
        {
            RoutedCommand.PathAndQuery = command.PathAndQuery
            RoutedCommand.HttpMethod = command.HttpMethod
            RoutedCommand.Exec = 
                (fun (wrappedTarget: obj) (request: HttpRequest) ->
                    command.Exec (wrappedTarget :?> 'Command) request
                )
        }

    let create (config: CommandWebHostConfig<'Events>): IWebHost =
        let handlers = 
            config.Commands
            |> List.map Routing.createHandler
            
        let startupConfig:CommandWebHostStartupConfig<'Events> =
            {
                Logger = config.Logger
                Handlers = handlers
                OnHandle = config.OnHandle
            }

        WebHostBuilder()
            .ConfigureServices(fun services -> services.AddSingleton<CommandWebHostStartupConfig<'Events>>(startupConfig) |> ignore)
            .UseKestrel()
            .UseStartup<CommandWebHostStartup<'Events>>()
            .UseUrls(config.BaseUri |> string)
            .Build()
      