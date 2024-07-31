[<AutoOpen>]
module Helpers

open System.Threading.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open System.IO
open System.Net.Http
open System.Text
open NSubstitute
open System.Collections.Generic
open Microsoft.AspNetCore.Http.Features
open Expecto.Tests
open Expecto
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.TestHost
open Saturn.Application
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

type IDependency =
  abstract member Call : unit -> unit
  abstract member Value : unit -> int

let dependency () =
  let mutable counter = 0
  {new IDependency with
    member __.Call () =
      counter <- counter + 1
      ()
    member __.Value() =
      counter
  }

let getEmptyContext (method: string) (path : string) =
  let ctx = Substitute.For<HttpContext>()
  ctx.Request.Method.ReturnsForAnyArgs method |> ignore

  ctx.Request.Scheme.ReturnsForAnyArgs ("http") |> ignore
  ctx.Request.Host.ReturnsForAnyArgs (HostString("localhost")) |> ignore
  ctx.Request.PathBase.ReturnsForAnyArgs(PathString "") |> ignore
  ctx.Request.Path.ReturnsForAnyArgs (PathString(path)) |> ignore
  ctx.Request.QueryString.ReturnsForAnyArgs (QueryString "") |>ignore

  // Essential for Giraffe subrouting work.
  ctx.Items.ReturnsForAnyArgs(Dictionary<_,_>()) |> ignore

  // For posterity, not needed, yet.
  ctx.Features.ReturnsForAnyArgs(FeatureCollection()) |> ignore

  ctx.Response.Body <- new MemoryStream()

  ctx.RequestServices
     .GetService(typeof<Json.ISerializer>)
     .Returns(NewtonsoftJson.Serializer(NewtonsoftJson.Serializer.DefaultSettings))
    |> ignore

  ctx.RequestServices
    .GetService(typeof<IDependency>)
    .Returns(dependency ())
    |> ignore

  ctx.RequestServices
    .GetService(typeof<INegotiationConfig>)
    .Returns(DefaultNegotiationConfig())
    |> ignore

  ctx

let testHostWithContext (host: IHost) (ctx: HttpContext) =
  use host =
    host.StartAsync () |> Async.AwaitTask |> Async.RunSynchronously
    host
  use server = host.GetTestServer()
  let result = server.SendAsync(fun c ->
    c.Request.Method <- ctx.Request.Method
    c.Request.Scheme  <- ctx.Request.Scheme
    c.Request.Host  <- ctx.Request.Host
    c.Request.PathBase  <- ctx.Request.PathBase
    c.Request.Path  <- ctx.Request.Path
    c.Request.QueryString  <- ctx.Request.QueryString
  )
  result.Result

let next : HttpFunc = Some >> Task.FromResult

let runTask task =
  task
  |> Async.AwaitTask
  |> Async.RunSynchronously

let getContentType (response : HttpResponse) =
  response.Headers.["Content-Type"].[0]

let getStatusCode (ctx : HttpContext) =
  ctx.Response.StatusCode

let getBody (ctx : HttpContext) =

  ctx.Response.Body.Position <- 0L
  use reader = new StreamReader(ctx.Response.Body, Encoding.UTF8)
  reader.ReadToEnd()

let getBody' (ctx : HttpContext) =

  use reader = new StreamReader(ctx.Response.Body, Encoding.UTF8)
  reader.ReadToEnd()

let readText (response : HttpResponseMessage) =
  response.Content.ReadAsStringAsync()
  |> runTask

let readBytes (response : HttpResponseMessage) =
  response.Content.ReadAsByteArrayAsync()
  |> runTask

let expectResponse expected actual =
  match actual with
  | None -> failtestf "Result was expected to be %s, but was %A" expected actual
  | Some ctx ->
      Expect.equal (getBody ctx) expected "Result should be equal"

let responseTestCase handler method path expected () =
  let ctx = getEmptyContext method path

  try
      let result = handler next ctx |> runTask
      expectResponse expected result
  with ex -> failtestf "failed because %A" ex

let responseEndpointTestCase host method path expected () =
  try
    let ctx = getEmptyContext method path
    let res = testHostWithContext host ctx
    Expect.equal (getBody' res) expected "Result should be equal"
  with ex -> failtestf "failed because %A" ex

let hostFromController ctr =
  let app = application {
    use_endpoint_router ctr
    webhost_config (fun hs -> hs.UseTestServer ())
    logging (fun lg -> lg.ClearProviders() |> ignore)
    service_config (fun sc -> sc.AddSingleton<IDependency>(dependency ()))
  }
  app.Build()

let hostFromControllerCached ctr cacheValues =
  let app = application {
    use_endpoint_router ctr
    webhost_config (fun hs -> hs.UseTestServer ())
    logging (fun lg -> lg.ClearProviders() |> ignore)
    service_config (fun sc -> sc.AddSingleton<IDependency>(dependency ()))
    use_static "static" cacheValues
  }
  app.Build()
