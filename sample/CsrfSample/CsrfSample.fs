module Sample

open Saturn
open Giraffe.ResponseWriters
open Microsoft.Extensions.Logging

//TODO
// module Views =
//   open Microsoft.AspNetCore.Http
//   open Giraffe.GiraffeViewEngine
//   open Saturn.CSRF.View.Giraffe

//   let index (ctx: HttpContext) =
//     body [] [
//       h1 [] [
//         rawText "Hello from static!"
//       ]
//       protectedForm ctx [ _action "/form"; _method "post" ] [
//         input [_type "submit"; _value "Yay" ]
//       ]
//     ]

/// There are two routes on this router: one retrieves the token(s) and tells you the form fields/request headers to send the request token on.
/// The other requires the token to be present before returning a success message to you.
let appRouter = router {
  pipe_through protectFromForgery

  //TODO
  // get "/" (fun next ctx -> htmlView (Views.index ctx) next ctx)
  get "/csrftoken" (fun next ctx -> json (CSRF.getRequestTokens ctx) next ctx)
  post "/requiresToken" (text "you gave a token!")
  post "/form" (fun next ctx -> json ctx.Request.Form next ctx)
}

let pipeline = pipeline {
  plug fetchSession
  plug requestId
}

/// adding the antiforgery plug to your application automatically configures the Asp.Net Core data protection pipeline. Any customizations you do to data protection (keys, storage, etc) will be used automatically.
let app = application {
  logging (fun (builder: ILoggingBuilder) -> builder.SetMinimumLevel(LogLevel.Trace) |> ignore)
  pipe_through pipeline
  use_antiforgery

  use_router appRouter
  url "http://0.0.0.0:8085/"
  memory_cache
  use_gzip
}

[<EntryPoint>]
let main _ =
  run app
  0 // return an integer exit code
