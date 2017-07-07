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

type CommandWebHostStartupConfig<'PartitionId, 'State, 'Events> = {
    Logger: Sunergeo.Logging.Logger
    ContextProvider: HttpContext -> Context
    Handlers: CommandHandler<'State, 'Events> list
    OnHandle: 'PartitionId -> Context -> CommandResult<'State, 'Events> -> unit
}

type CommandWebHostStartup<'PartitionId, 'State, 'Events when 'PartitionId : comparison> (config: CommandWebHostStartupConfig<'PartitionId, 'State, 'Events>) = 

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
                        events |> config.OnHandle (Sunergeo.Core.Todo.todo()) context
                        None
                    )
                    (fun logLevel message -> 
                        config.Logger logLevel (sprintf "%O -> %s" ctx.Request.Path message)
                    )

            } |> Async.StartAsTask :> Task

        app.Run(RequestDelegate(reqHandler))

type CommandWebHostConfig<'PartitionId, 'State, 'Events> = {
    Logger: Sunergeo.Logging.Logger
    Handlers: CommandHandler<'State, 'Events> list
    OnHandle: 'PartitionId -> Context -> CommandResult<'State, 'Events> -> unit
    BaseUri: Uri
}

module CommandWebHost =
    let toGeneralRoutedCommand<'Command, 'State, 'Events>
        (routedCommand: RoutedCommand<'Command, 'State, 'Events>)
        :RoutedCommand<obj, 'State, 'Events> =
        {
            RoutedCommand.PathAndQuery = routedCommand.PathAndQuery
            RoutedCommand.HttpMethod = routedCommand.HttpMethod
            RoutedCommand.Exec = 
                (fun (wrappedTarget: obj) (context: Context) ->
                    routedCommand.Exec (wrappedTarget :?> 'Command) context
                )
        }

    let create<'PartitionId, 'State, 'Events when 'PartitionId : comparison> (config: CommandWebHostConfig<'PartitionId, 'State, 'Events>): IWebHost =
            
        let startupConfig:CommandWebHostStartupConfig<'PartitionId, 'State, 'Events> =
            {
                Logger = config.Logger
                Handlers = config.Handlers
                OnHandle = config.OnHandle
                ContextProvider = 
                    (fun _ -> 
                        {
                            // TODO:
                            Context.UserId = ""
                            Context.WorkingAsUserId = ""
                            Context.Timestamp = NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow)
                        }
                    )
            }

        WebHostBuilder()
            .ConfigureServices(fun services -> services.AddSingleton<CommandWebHostStartupConfig<'PartitionId, 'State, 'Events>>(startupConfig) |> ignore)
            .UseKestrel()
            .UseStartup<CommandWebHostStartup<'PartitionId, 'State, 'Events>>()
            .UseUrls(config.BaseUri |> string)
            .Build()
      