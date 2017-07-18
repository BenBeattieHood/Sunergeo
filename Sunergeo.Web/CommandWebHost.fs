namespace Sunergeo.Web.Commands

open System
open Sunergeo.Core
open Sunergeo.Web

open Routing

//type LogConfig = {
//    LogName: string
//}

type RoutedCommand<'Command, 'PartitionId when 'Command :> ICommandBase<'PartitionId> and 'PartitionId : comparison> =
    RoutedType<'Command, unit>

//type RoutedCommand<'Command, 'PartitionId, 'State, 'Events when 'Command :> IUpdateCommand<'PartitionId, 'State, 'Events> and 'PartitionId : comparison> =
//    RoutedType<'Command, UpdateCommandResult<'Events>>

type CommandHandler = RoutedTypeRequestHandler<unit>

open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Sunergeo.Logging
open Microsoft.Extensions.DependencyInjection

//, 'CreateCommand, 'UpdateCommand and 'CreateCommand :> ICreateCommand<'PartitionId, 'Events> and 'UpdateCommand :> IUpdateCommand<'PartitionId, 'State, 'Events>
type CommandWebHostStartupConfig<'PartitionId, 'State, 'Events when 'PartitionId : comparison> = {
    Logger: Sunergeo.Logging.Logger
    ContextProvider: HttpContext -> Context
    Handlers: CommandHandler list
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

                let commandHandler =
                    config.Handlers
                    |> List.tryPick
                        (fun handler ->
                            handler context ctx.Request
                        )
                        
                WebHost.runHandlerAndOutputToResponse 
                    commandHandler
                    (fun _ -> None)
                    (fun logLevel message -> 
                        config.Logger logLevel (sprintf "%O -> %s" ctx.Request.Path message)
                    )
                    ctx.Response

            } |> Async.StartAsTask :> Task

        app.Run(RequestDelegate(reqHandler))

type CommandWebHostConfig<'PartitionId, 'State, 'Events> = {
    Logger: Sunergeo.Logging.Logger
    Handlers: CommandHandler list
    BaseUri: Uri
}

module CommandWebHost =
    let toGeneralRoutedCommand<'Command, 'PartitionId when 'Command :> ICommandBase<'PartitionId> and 'PartitionId : comparison>
        (routedCommand: RoutedCommand<'Command, 'PartitionId>)
        :RoutedCommand<ICommandBase<'PartitionId>, 'PartitionId> =
        {
            RoutedCommand.PathAndQuery = routedCommand.PathAndQuery
            RoutedCommand.HttpMethod = routedCommand.HttpMethod
            RoutedCommand.Exec = 
                (fun (wrappedTarget: ICommandBase<'PartitionId>) (context: Context) ->
                    routedCommand.Exec (wrappedTarget :?> 'Command) context
                )
        }

    let create<'PartitionId, 'State, 'Events when 'PartitionId : comparison> (config: CommandWebHostConfig<'PartitionId, 'State, 'Events>): IWebHost =
            
        let startupConfig:CommandWebHostStartupConfig<'PartitionId, 'State, 'Events> =
            {
                Logger = config.Logger
                Handlers = config.Handlers
                ContextProvider = 
                    (fun httpContext -> 
                        {
                            Context.UserId = Sunergeo.Core.Todo.todo()
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
      