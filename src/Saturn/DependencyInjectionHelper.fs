module Saturn.DependencyInjectionHelper

open System
open System.Collections.Concurrent
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.FSharp.Reflection

let private constructors = ConcurrentDictionary<Type, IServiceProvider -> obj>()

let buildDependencies<'Dependencies> (svcs: IServiceProvider) =
  let getService (svcs: IServiceProvider) (t: Type) =
    let res = svcs.GetService(t)
    if isNull res then failwithf "No service for type '%s' has been registered" t.FullName
    res

  let getCtor (t: Type) : IServiceProvider -> obj =
    if FSharpType.IsTuple t then
      let fields = FSharpType.GetTupleElements t
      fun svcs ->
        let fields = fields |> Array.map (getService svcs)
        FSharpValue.MakeTuple(fields, t)
    elif t = typeof<obj> then
      fun _ -> obj()
    elif t = typeof<unit> then
      fun _ -> box ()
    else
      fun svcs -> getService svcs t

  let ctor = constructors.GetOrAdd(typeof<'Dependencies>, Func<_,_> getCtor)
  ctor svcs |> unbox<'Dependencies>

let withInjectedDependencies (handler: 'Dependencies -> HttpHandler) =
  fun nxt (ctx: HttpContext) ->
    (handler <| buildDependencies<'Dependencies> ctx.RequestServices) nxt ctx

let withInjectedDependenciesp1 (handler: 'Dependencies -> 'a -> HttpHandler) =
  fun p1 nxt (ctx: HttpContext) ->
    (handler <| buildDependencies<'Dependencies> ctx.RequestServices) p1 nxt ctx

let withInjectedDependenciesp2 (handler: 'Dependencies -> 'a -> 'b -> HttpHandler) =
  fun p1 p2 nxt (ctx: HttpContext) ->
    (handler <| buildDependencies<'Dependencies> ctx.RequestServices) p1 p2 nxt ctx

let withInjectedDependenciesp3 (handler: 'Dependencies -> 'a -> 'b -> 'c -> HttpHandler) =
  fun p1 p2 p3 nxt (ctx: HttpContext) ->
    (handler <| buildDependencies<'Dependencies> ctx.RequestServices) p1 p2 p3 nxt ctx

let mapFromHttpContext (f: HttpContext -> 'Dependencies -> 'Result) : HttpContext -> 'Result =
  fun ctx ->
    f ctx <| buildDependencies<'Dependencies> ctx.RequestServices

let mapFromServiceProvider (f: IServiceProvider -> 'Dependencies -> 'Result) : IServiceProvider -> 'Result =
  fun ctx ->
    f ctx <| buildDependencies<'Dependencies> ctx
