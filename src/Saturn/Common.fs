namespace Saturn

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