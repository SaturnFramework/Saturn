namespace Saturn

open System
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives

[<AutoOpen>]
module Common =
  open Giraffe
  open FSharp.Control.Tasks.V2.ContextInsensitive

  [<RequireQualifiedAccess>]
  type InclusiveOption<'T> =
  | None
  | Some of 'T
  | All

  let inline internal succeed nxt cntx  = nxt cntx
  let internal abort : HttpFuncResult = System.Threading.Tasks.Task.FromResult None

  let inline internal halt ctx = task { return Some ctx }

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

  let private handlerWithRootedPath (path : string) (handler : HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
      task {
          let savedSubPath = getSavedSubPath ctx
          ctx.Items.Item RouteKey <- ((savedSubPath |> Option.defaultValue "") + path)
          let! result = handler next ctx
          match result with
          | Some _ -> ()
          | None ->
            match savedSubPath with
            | Some savedSubPath -> ctx.Items.Item   RouteKey <- savedSubPath
            | None              -> ctx.Items.Remove RouteKey |> ignore
          return result
      }

  let private getPath (ctx : HttpContext) =
    match getSavedSubPath ctx with
    | Some p when ctx.Request.Path.Value.Contains p -> ctx.Request.Path.Value.[p.Length..]
    | _   -> ctx.Request.Path.Value

  let internal routefUnsafe (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        Giraffe.FormatExpressions.tryMatchInput path (getPath ctx) false
        |> function
            | None      -> System.Threading.Tasks.Task.FromResult None
            | Some args -> routeHandler args next ctx

  let internal subRoutefUnsafe (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let paramCount   = (path.Value.Split '/').Length
            let subPathParts = (getPath ctx).Split '/'
            if paramCount > subPathParts.Length then abort
            else
                let subPath =
                    subPathParts
                    |> Array.take paramCount
                    |> Array.fold (fun state elem ->
                        if String.IsNullOrEmpty elem
                        then state
                        else sprintf "%s/%s" state elem) ""
                Giraffe.FormatExpressions.tryMatchInput path subPath false
                |> function
                    | None      -> abort
                    | Some args -> handlerWithRootedPath subPath (routeHandler args) next ctx

  let internal routefUnsafeCi (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        Giraffe.FormatExpressions.tryMatchInput path (getPath ctx) true
        |> function
            | None      -> System.Threading.Tasks.Task.FromResult None
            | Some args -> routeHandler args next ctx

  let internal subRoutefUnsafeCi (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let paramCount   = (path.Value.Split '/').Length
            let subPathParts = (getPath ctx).Split '/'
            if paramCount > subPathParts.Length then abort
            else
                let subPath =
                    subPathParts
                    |> Array.take paramCount
                    |> Array.fold (fun state elem ->
                        if String.IsNullOrEmpty elem
                        then state
                        else sprintf "%s/%s" state elem) ""
                Giraffe.FormatExpressions.tryMatchInput path subPath true
                |> function
                    | None      -> abort
                    | Some args -> handlerWithRootedPath subPath (routeHandler args) next ctx

  let internal subRoutefCi (path : PrintfFormat<_,_,_,_, 'T>) (routeHandler : 'T -> HttpHandler) : HttpHandler =
        Giraffe.FormatExpressions.validateFormat path
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let paramCount   = (path.Value.Split '/').Length
            let subPathParts = (SubRouting.getNextPartOfPath ctx).Split '/'
            if paramCount > subPathParts.Length then abort
            else
                let subPath =
                    subPathParts
                    |> Array.take paramCount
                    |> Array.fold (fun state elem ->
                        if String.IsNullOrEmpty elem
                        then state
                        else sprintf "%s/%s" state elem) ""
                Giraffe.FormatExpressions.tryMatchInput path subPath true
                |> function
                    | None      -> abort
                    | Some args -> SubRouting.routeWithPartialPath subPath (routeHandler args) next ctx