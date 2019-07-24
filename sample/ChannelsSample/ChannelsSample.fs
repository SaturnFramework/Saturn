module ChannelsSample

open Saturn
open Giraffe.ResponseWriters
open Giraffe.Core
open FSharp.Control.Tasks.V2
open Saturn.Channels
open Microsoft.Extensions.Logging
open Giraffe.HttpStatusCodeHandlers


let browserRouter = router {
    get "/" (text "Hello world")
    get "/ping" (fun next ctx -> task {
      let hub = ctx.GetService<Saturn.Channels.ISocketHub>()
      match ctx.TryGetQueryStringValue "message" with
      | None ->
        do! hub.SendMessageToClients "/channel" "greeting" "hello"
      | Some message ->
        do! hub.SendMessageToClients "/channel" "greeting" (sprintf "hello, %s" message)
      return! Successful.ok (text "Pinged the clients") next ctx
    })
}

let sampleChannel = channel {
    join (fun ctx id -> task {
      ctx.GetLogger().LogInformation("Connected! Socket Id: " + id.ToString())
      return Ok
    })

    handle "topic" (fun ctx msg ->
        task {
            let logger = ctx.GetLogger()
            logger.LogInformation("got message {message} from client", msg)
            return ()
        }
    )
}


let app = application {
    use_router browserRouter
    url "http://localhost:8085/"
    add_channel "/channel" sampleChannel
}

[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code
