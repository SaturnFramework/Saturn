open Giraffe
open Saturn
open Microsoft.Extensions.Logging
open Saturn.Endpoint

let topRouter = router {
    get "/" (text "")
    getf "/user/%s" text
    post "/user" (text "")

}

let app = application {
  use_endpoint_router topRouter
  logging (fun loggerBuilder ->
    loggerBuilder.ClearProviders()
    |> ignore
  )
}

[<EntryPoint>]
let main _ =
    run app
    0
