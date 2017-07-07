namespace Sunergeo.Web
type RouteAttribute(pathAndQuery:string, httpMethod: HttpMethod) =
    inherit System.Attribute()
    member this.PathAndQuery = pathAndQuery
    member this.HttpMethod = httpMethod