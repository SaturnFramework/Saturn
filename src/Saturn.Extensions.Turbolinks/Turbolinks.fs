module Saturn

open Saturn
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open System.Threading.Tasks
open Giraffe
open Microsoft.Extensions.Primitives

module TurbolinksHelpers =
  let isXhr (ctx: HttpContext) =
    ctx.Request.Headers.["X-Requested-With"].ToString() = "XMLHttpRequest"

  let isTurbolink (ctx: HttpContext) =
    ctx.Request.Headers.ContainsKey "Turbolinks-Referrer"

  let js (ctx: HttpContext, data) =
    ctx.SetContentType "text/javascript"
    ctx.WriteStringAsync data

  let internal turbolinksResp (t, m) =
    if m = "GET" then
      sprintf "Turbolinks.visit('%s');" t
    else
      sprintf "Turbolinks.clearCache();\nTurbolinks.visit('%s');" t

  let redirect ctx path : HttpFuncResult=
    if isXhr ctx then
      ctx.SetStatusCode 200
      js(ctx, turbolinksResp(path, ctx.Request.Method) )
    else
      Controller.redirect ctx path

  let internal handleTurbolinks (ctx: HttpContext) =
    if isTurbolink ctx then
      ctx.Response.Headers.Add("Turbolinks-Location", StringValues ctx.Request.Path.Value)

///HttpHandler enabling Turbolinks support for given pipelines
let turbolinks (nxt : HttpFunc) (ctx : HttpContext) : HttpFuncResult =
  TurbolinksHelpers.handleTurbolinks ctx
  nxt ctx

type TurbolinksMiddleware (next: RequestDelegate) =
  member __.Invoke(ctx: HttpContext) =
    ctx.Response.OnStarting((fun ctx ->
      TurbolinksHelpers.handleTurbolinks (unbox<_> ctx)
      Task.CompletedTask
    ), ctx)
    next.Invoke(ctx)

type Saturn.Application.ApplicationBuilder with

  [<CustomOperation("use_turbolinks")>]
  ///Enable turbolinks supports for whole application (all endpoints)
  member __.UseTurbolinks(state) =
      let middleware (app : IApplicationBuilder) =
        app.UseMiddleware<TurbolinksMiddleware>()

      { state with AppConfigs = middleware::state.AppConfigs }


