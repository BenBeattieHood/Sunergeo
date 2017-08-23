module Routing

open System
open System.Text.RegularExpressions

open Sunergeo.Core
open Sunergeo.Web
open Microsoft.AspNetCore.Http


type RoutedType<'HandlerType, 'Result> = {
    PathAndQuery: string
    HttpMethod: HttpMethod
    Exec: 'HandlerType -> Context -> HttpRequest -> Async<Result<'Result, Error>>
}

type RoutedTypeRequestHandler<'Result> = 
    Context -> Microsoft.AspNetCore.Http.HttpRequest -> Option<unit -> Async<Result<'Result, Error>>>


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
        (fun s -> Sunergeo.Core.Todo.todo<obj>())


let createHandler<'TargetType, 'Result>
    (routedType: RoutedType<'TargetType, 'Result>)
    :RoutedTypeRequestHandler<'Result> =
    let pathAndQueryRegexString = routedType.PathAndQuery |> routePathAndQueryToRegexString
    let pathAndQueryRegex = Regex(pathAndQueryRegexString, RegexOptions.Compiled)

    let t = typeof<'TargetType>
    let ctor = t.GetConstructors().[0]      // assume a record type
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
            
    (fun (context: Context) (request: Microsoft.AspNetCore.Http.HttpRequest) ->
        
        if (request.Method |> HttpMethod.fromString) <> routedType.HttpMethod
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
                    let targetActivator =
                        Sunergeo.Core.Reflection.getActivator<'TargetType>
                            ctor
                            ctorParams

                    let target =
                        ctorParamValues
                        |> targetActivator.Invoke
                        
                    (fun _ -> routedType.Exec target context request)
                    |> Some
                else
                    None
            else 
                None
    )