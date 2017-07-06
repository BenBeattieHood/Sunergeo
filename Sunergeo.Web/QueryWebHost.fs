namespace Sunergeo.Web.Queries

open System
open Sunergeo.Core
open Sunergeo.Web

open Routing

//type LogConfig = {
//    LogName: string
//}


open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Sunergeo.Logging
open Microsoft.Extensions.DependencyInjection;

type RoutedQuery<'Query> = RoutedType<'Query, obj>

type QueryHandler = RoutedTypeRequestHandler<obj>

type QueryWebHostStartupConfig = {
    Logger: Sunergeo.Logging.Logger
    Handlers: QueryHandler list
}

type QueryWebHostStartup (config: QueryWebHostStartupConfig) = 

    member x.Configure 
        (
            app: IApplicationBuilder, 
            hosting: IHostingEnvironment
        ): unit =

        let reqHandler (ctx: HttpContext) = 
            async {
                sprintf "Received %O" ctx.Request.Path
                |> config.Logger Sunergeo.Logging.LogLevel.Information

                //let result =
                //    config.Handlers
                //    |> List.tryPick
                //        (fun handler ->
                //            handler ctx.Request
                //        )
                        



            } |> Async.StartAsTask :> Task

        app.Run(RequestDelegate(reqHandler))
        

type QueryWebHostConfig = {
    Logger: Sunergeo.Logging.Logger
    Queries: RoutedQuery<obj> list
    BaseUri: Uri
}

module QueryWebHost =
    let toGeneralRoutedQuery<'Query>
        (command: RoutedQuery<'Query>)
        :RoutedQuery<obj> =
        {
            RoutedQuery.PathAndQuery = command.PathAndQuery
            RoutedQuery.HttpMethod = command.HttpMethod
            RoutedQuery.Exec = 
                (fun (wrappedTarget: obj) (request: HttpRequest) ->
                    command.Exec (wrappedTarget :?> 'Query) request
                )
        }

    let create (config: QueryWebHostConfig): IWebHost =
        let handlers = 
            config.Queries
            |> List.map createHandler
            
        let startupConfig =
            {
                QueryWebHostStartupConfig.Logger = config.Logger
                QueryWebHostStartupConfig.Handlers = handlers
            }

        WebHostBuilder()
            .ConfigureServices(fun services -> services.AddSingleton<QueryWebHostStartupConfig>(startupConfig) |> ignore)
            .UseKestrel()
            .UseStartup<QueryWebHostStartup>()
            .UseUrls(config.BaseUri |> string)
            .Build()
      