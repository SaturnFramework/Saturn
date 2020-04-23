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
open Giraffe.Serialization

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
     .GetService(typeof<Json.IJsonSerializer>)
     .Returns(new NewtonsoftJsonSerializer(NewtonsoftJsonSerializer.DefaultSettings))
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
