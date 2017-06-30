namespace Sunergeo.Web
type RouteAttribute(uri:string) =
    inherit System.Attribute()
    member this.Uri = uri