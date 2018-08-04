module Saturn

open System
open Saturn
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open System.IO
open FSharp.Control.Tasks.ContextInsensitive
open System.Threading.Tasks
open Giraffe

module TurnolinksHelpers =
    let isXhr (ctx: HttpContext) =
        ctx.Request.Headers.["X-Requested-With"].ToString() = "XMLHttpRequest"


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


type Turbolinks (next: RequestDelegate) =





    member __.Invoke(ctx: HttpContext) =
        task {
            let ms = new MemoryStream()
            let bs = ctx.Response.Body
            ctx.Response.Body <- ms
            do! next.Invoke(ctx)
            let req = ctx.Request
            let res = ctx.Response
            if not (String.IsNullOrWhiteSpace (req.Headers.["Turbolinks-Referrer"].ToString())) then
                let co = CookieOptions()
                co.HttpOnly <- false
                ctx.Response.Cookies.Append("request_method", req.Method, co)
                if ctx.Response.StatusCode = 301 || ctx.Response.StatusCode = 302 then
                    let uri = Uri(res.Headers.["Location"].ToString())
                    if uri.Host.Equals(req.Host.Value) then
                        res.Headers.["Turbolinks-Location"] <- res.Headers.["Location"]

            ms.WriteTo bs
            do! bs.FlushAsync ()
            ms.Dispose ()
            bs.Dispose ()
            return ()
        } :> Task

type ApplicationBuilder with

    [<CustomOperation("use_turbolinks")>]
    member __.UseTurbolinks(state) =
        let middleware (app : IApplicationBuilder) =
            app.UseMiddleware<Turbolinks>()

        { state with
            AppConfigs = middleware::state.AppConfigs
        }


