namespace Saturn

open System
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

[<AutoOpen>]
module Common =
  open Giraffe

  [<RequireQualifiedAccess>]
  type InclusiveOption<'T> =
  | None
  | Some of 'T
  | All

  let inline internal succeed nxt cntx  = nxt cntx

  let inline internal halt _ ctx = task {return Some ctx }

  let internal get<'a> v (ctx : HttpContext) =
    match ctx.Items.TryGetValue v with
    | true, l -> unbox<'a> l |> Some
    | _ -> None

  let internal setHttpHeaders (vals: (string * string) list ) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        vals |> List.iter (fun (key, value) ->
          ctx.Response.Headers.[key] <- StringValues(value))
        next ctx

  [<Literal>]
  let private RouteKey = "giraffe_route"

  let private getSavedSubPath (ctx : HttpContext) =
    let inline strOption (str : string) =
      if String.IsNullOrEmpty str then None else Some str
    if ctx.Items.ContainsKey RouteKey
    then ctx.Items.Item RouteKey |> string |> strOption
    else None

  let private getPath (ctx : HttpContext) =
    match getSavedSubPath ctx with
    | Some p when ctx.Request.Path.Value.Contains p -> ctx.Request.Path.Value.[p.Length..]
    | _   -> ctx.Request.Path.Value

  let routefUnsafe (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        Giraffe.FormatExpressions.tryMatchInput path (getPath ctx) false
        |> function
            | None      -> System.Threading.Tasks.Task.FromResult None
            | Some args -> routeHandler args next ctx