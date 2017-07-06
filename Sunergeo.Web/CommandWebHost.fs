namespace Sunergeo.Web.Commands

open System
open Sunergeo.Core
open Sunergeo.Web

//type LogConfig = {
//    LogName: string
//}

type CommandRequestHandler = 
    Microsoft.AspNetCore.Http.HttpRequest -> (Result<unit, Error> option)

type CommandWebHostStartupConfig = {
    Logger: Sunergeo.Logging.Logger
    Handlers: CommandRequestHandler list
}


open System.Threading.Tasks
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Sunergeo.Logging
open Microsoft.Extensions.DependencyInjection;


type CommandWebHostStartup (config: CommandWebHostStartupConfig) = 

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
                    | ErrorStatus.InvalidOp ->
                        ctx.Response.StatusCode <- StatusCodes.Status400BadRequest
                        
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
        
        
type RoutedCommand = {
    PathAndQuery: string
    HttpMethod: HttpMethod
    CommandType: Type
}

type CommandWebHostConfig = {
    Logger: Sunergeo.Logging.Logger
    Commands: RoutedCommand list
    BaseUri: Uri
}

module CommandWebHost =
    open System.Text.RegularExpressions
    
    let routePathAndQueryVariableRegex = Regex(@"\{(.+?)\}", RegexOptions.Compiled)
    let routePathAndQueryToRegexString
        (path: string)
        :string =
        routePathAndQueryVariableRegex.Replace 
            (
                path,
                (fun x ->
                    let name = 
                        if x.Value = "{_}" 
                        then x.Index |> string
                        else x.Groups.[1].Value
                    sprintf "(?<%s>.+?)" name
                )
            )

    let createUriPathOrQueryParamParser
        (targetType: Type)
        :(string -> obj) =
        if targetType = typeof<string> then
            (fun s -> upcast s)
        elif targetType = typeof<Int32> then
            (fun s -> upcast (Int32.Parse s))
        elif targetType = typeof<Boolean> then
            (fun s -> upcast (s = "true" || s = "1"))
        elif targetType = typeof<Double> then
            (fun s -> upcast (Double.Parse s))
        else
            failwith "TODO"

    let createHandler 
        (command: RoutedCommand)
        :CommandRequestHandler =
        let pathAndQueryRegexString = command.PathAndQuery |> routePathAndQueryToRegexString
        let pathAndQueryRegex = Regex(pathAndQueryRegexString, RegexOptions.Compiled)

        let ctor = command.CommandType.GetConstructors().[0]      // assume a record type
        let ctorParams = ctor.GetParameters()

        let regexGroupNames = 
            pathAndQueryRegex.GetGroupNames()
            |> Array.filter (fun x -> x <> "0") // filter out the default group name

        let ctorParamToPathAndQueryRegexMapping = 
            regexGroupNames
            |> Array.map
                (fun regexGroupName ->
                    let ctorParamIndex =
                        ctorParams
                        |> Array.tryFindIndex
                            (fun ctorParam ->   // will also validate that all regex params are covered
                                String.Equals(regexGroupName, ctorParam.Name, StringComparison.InvariantCultureIgnoreCase)
                            )

                    match ctorParamIndex with
                    | Some ctorParamIndex ->
                        let ctorParam = 
                            ctorParams
                            |> Array.item ctorParamIndex
                            
                        ctorParamIndex, (regexGroupName, ctorParam.ParameterType |> createUriPathOrQueryParamParser)
                    | None ->
                        failwith (sprintf "Unknown route parameter {%s}" regexGroupName)
                )
            |> Map.ofArray
            
        (fun (request: HttpRequest) ->
        
            if (request.Method |> HttpMethod.fromString) <> command.HttpMethod
            then
                None
            else
                let pathAndQueryRegexValues = 
                    pathAndQueryRegex.Match(request.Path + request.QueryString).Groups
                
                let pathAndQueryParams =
                    regexGroupNames
                    |> Array.choose
                        (fun regexGroupName ->
                            let group = pathAndQueryRegexValues.Item(regexGroupName)
                            if group.Captures.Count = 1
                            then
                                (regexGroupName, group.Value)
                                |> Some
                            else
                                None
                        )
                    |> Map.ofArray

                if (pathAndQueryParams |> Map.count) = (ctorParamToPathAndQueryRegexMapping |> Map.count)
                then
                    let ctorParamValues =
                        ctorParams
                        |> Array.mapi
                            (fun index ctorParam ->
                                let ctorParamToPathAndQueryRegexGroupName =
                                    ctorParamToPathAndQueryRegexMapping
                                    |> Map.tryFind index

                                match ctorParamToPathAndQueryRegexGroupName with
                                | Some (ctorParamToPathAndQueryRegexGroupName, uriPathOrQueryParamParser) ->
                                    pathAndQueryParams
                                    |> Map.find ctorParamToPathAndQueryRegexGroupName
                                    |> uriPathOrQueryParamParser
                                    |> Some
                                | None ->
                                    None    /// TODO
                            )
                        |> Array.choose id

                    if (ctorParamValues |> Array.length) = (ctorParams |> Array.length)
                    then
                        let command =
                            (ctorParamValues |> ctor.Invoke)

                        () |> Result.Ok |> Some
                    else
                        None
                else 
                    None
        )

    let create (config: CommandWebHostConfig): IWebHost =
        let handlers = 
            config.Commands
            |> List.map createHandler
            
        let startupConfig =
            {
                CommandWebHostStartupConfig.Logger = config.Logger
                CommandWebHostStartupConfig.Handlers = handlers
            }

        WebHostBuilder()
            .ConfigureServices(fun services -> services.AddSingleton<CommandWebHostStartupConfig>(startupConfig) |> ignore)
            .UseKestrel()
            .UseStartup<CommandWebHostStartup>()
            .UseUrls(config.BaseUri |> string)
            .Build()
      