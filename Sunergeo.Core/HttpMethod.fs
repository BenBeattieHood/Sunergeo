namespace Sunergeo.Web
type HttpMethod =
    Get = 0
    | Patch = 1
    | Post = 2
    | Put = 3
    | Delete = 4
    
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module HttpMethod =
    let fromString(s:string):HttpMethod =
        match s with
        | "GET" -> HttpMethod.Get
        | "PATCH" -> HttpMethod.Patch
        | "PUT" -> HttpMethod.Put
        | "POST" -> HttpMethod.Post
        | "DELETE" -> HttpMethod.Delete
        | _ -> invalidArg "s" (sprintf "Unsupported HTTP method '%s'" s) |> raise
