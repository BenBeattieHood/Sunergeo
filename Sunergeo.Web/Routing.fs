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


let routePathVariableRegex = Regex(@"\{(.+?)\}", RegexOptions.Compiled)
let routePathToRegexString
    (path: string)
    :string =
    routePathVariableRegex.Replace 
        (
            path,
            (fun x ->
                let name = 
                    if x.Value = "{_}" 
                    then x.Index |> string
                    else 
                        let result = x.Groups.[1].Value
                        sprintf "%c%s" (result.Chars(0) |> Char.ToLowerInvariant) (result.Substring(1))
                sprintf "(?<%s>.+?)" name
            )
        )
    |> sprintf "^%s$"


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
    let routedTypePath, routedTypeQueryOption = 
        let indexOfQuery = routedType.PathAndQuery.IndexOf('?')
        if indexOfQuery >= 0 
        then
            routedType.PathAndQuery.Substring(0, indexOfQuery),
            routedType.PathAndQuery.Substring(indexOfQuery + 1) |> Some
        else
            routedType.PathAndQuery,
            None
            
    let requestPathRegexString = routedTypePath |> routePathToRegexString
    let requestPathRegex = Regex(requestPathRegexString, RegexOptions.Compiled)

    let requestPathRegexGroupNames = 
        requestPathRegex.GetGroupNames()
        |> Array.filter (fun x -> x <> "0") // filter out the default group name

    let requestQueryParamNames =
        match routedTypeQueryOption with
        | Some routedTypeQuery ->
            routedTypeQuery.Split([| '&' |], StringSplitOptions.RemoveEmptyEntries)
            |> Array.map (fun x -> x.Split([| '=' |], StringSplitOptions.RemoveEmptyEntries) |> Array.head)
        | None ->
            Array.empty

    let t = typeof<'TargetType>
    let ctor = t.GetConstructors().[0]      // assume a record type
    let ctorParams = ctor.GetParameters()
    
    let _ =
        let isValidRequestParameter
            (requestParameter: string)
            : bool =
            ctorParams
            |> Array.exists
                (fun ctorParam ->
                    String.Equals(requestParameter, ctorParam.Name, StringComparison.InvariantCultureIgnoreCase)
                )

        let failRouteParam
            (message: string)
            =
            failwith (sprintf "%O @ \"%s\": %s" routedType.HttpMethod routedType.PathAndQuery message)

        let unrecognisedRequestPathGroupNames =
            requestPathRegexGroupNames 
            |> Array.filter
                (isValidRequestParameter >> not)

        if unrecognisedRequestPathGroupNames |> Array.length > 0 then
            sprintf "Unknown route path parameters %A" requestPathRegexGroupNames
            |> failRouteParam

        let unrecognisedRequestQueryParamNames =
            requestQueryParamNames
            |> Array.filter
                (isValidRequestParameter >> not)

        if unrecognisedRequestQueryParamNames |> Array.length > 0 then
            sprintf "Unknown route query parameter %A" unrecognisedRequestQueryParamNames
            |> failRouteParam

        let duplicateRequestParamNames =
            requestPathRegexGroupNames
            |> Seq.append requestQueryParamNames
            |> Seq.groupBy id
            |> Seq.map snd
            |> Seq.filter (fun x -> x |> Seq.length > 1)
            |> Seq.map Seq.head

        if duplicateRequestParamNames |> (not << Seq.isEmpty) then
            sprintf "Duplicated query params: %A" duplicateRequestParamNames
            |> failRouteParam

    let stringEquals
        (comparison: StringComparison)
        (a: string)
        (b: string)
        : bool =
        System.String.Equals(a, b, comparison)

    let requestPathOptic
        (ctorParamName: string)
        (map: string -> obj)
        (requestPathMatchGroupCollection: GroupCollection)
        : obj option =
        let group = requestPathMatchGroupCollection.Item(ctorParamName)

        if group.Captures.Count = 1
        then group.Value |> map |> Some
        else None

    let mapStringValues
        (map: string -> obj)
        (value: Microsoft.Extensions.Primitives.StringValues)
        : obj = 
        match value.ToArray() with
        | [||] -> () :> obj
        | [| item |] -> item |> map
        | values -> upcast (values |> Array.map map)

    let requestQueryParamOptic
        (ctorParamName: string)
        (map: string -> obj)
        (request: HttpRequest)
        : obj option =
        if ctorParamName |> request.Query.ContainsKey
        then 
            request.Query.[ctorParamName]
            |> mapStringValues map
            |> Some
        else 
            None

    let ctorParamOptics =
        ctorParams
        |> Array.map
            (fun ctorParam ->
                let parser = createUriPathOrQueryParamParser ctorParam.ParameterType
                
                if requestPathRegexGroupNames |> Array.exists (stringEquals StringComparison.InvariantCultureIgnoreCase ctorParam.Name) 
                then
                    requestPathOptic ctorParam.Name parser
                    |> (fun f x _ -> f x)
                    
                elif requestQueryParamNames |> Array.exists (stringEquals StringComparison.InvariantCultureIgnoreCase ctorParam.Name) 
                then
                    requestQueryParamOptic ctorParam.Name parser
                    |> (fun f _ y -> f y)

                else 
                    (fun 
                        _
                        request ->
                        match requestQueryParamOptic ctorParam.Name parser request with
                        | Some value -> value |> Some
                        | None ->
                            if ctorParam.Name |> request.Form.ContainsKey
                            then
                                request.Form.[ctorParam.Name]
                                |> mapStringValues parser
                                |> Some
                            else None
                    )
            )
            
    (fun (context: Context) (request: Microsoft.AspNetCore.Http.HttpRequest) ->
        
        if (request.Method |> HttpMethod.fromString) <> routedType.HttpMethod || request.Path.HasValue = false
        then
            None
        else
            let requestPathRegexValues = 
                requestPathRegex.Match(request.Path.Value).Groups

            let requestCtorValues =
                ctorParamOptics
                |> Array.choose
                    (fun ctorParamOptic ->
                        ctorParamOptic requestPathRegexValues request
                    )

            if requestCtorValues |> Array.length = ctorParams.Length
            then
                let targetActivator =
                    Sunergeo.Core.Reflection.getActivator<'TargetType>
                        ctor
                        ctorParams

                let target =
                    requestCtorValues
                    |> targetActivator.Invoke
                        
                (fun _ -> routedType.Exec target context request)
                |> Some
            else
                None
    )

let defaultContextProvider
    (instanceId: InstanceId)
    (httpContext: Microsoft.AspNetCore.Http.HttpContext)
    : Sunergeo.Core.Context =
    let fromCorrelationId =
        match "x-from-correlation-id" |> httpContext.Request.Headers.TryGetValue with
        | true, values 
            when values.Count > 0 && String.IsNullOrEmpty(values.[0]) = false -> 
                let result = values.[0]
                Some (CorrelationId(result))
        | _ -> 
                None
    {
        Context.InstanceId = instanceId
        Context.UserId = Sunergeo.Core.Todo.todo()
        Context.WorkingAsUserId = Sunergeo.Core.Todo.todo()
        Context.FromCorrelationId = fromCorrelationId
        Context.Timestamp = NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow)
    }