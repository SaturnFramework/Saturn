module ChannelsSample

open Saturn
open Giraffe.ResponseWriters
open Giraffe.Core
open FSharp.Control.Tasks.V2
open Saturn.Channels
open Microsoft.Extensions.Logging
open Giraffe.HttpStatusCodeHandlers

//Normal router for Http request, brodcasting messages to all connected clients
let browserRouter = router {
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

//Sample channel implementation
let sampleChannel = channel {
    join (fun ctx ci -> task {
      ctx.GetLogger().LogInformation("Connected! Socket Id: " + ci.SocketId.ToString())
      return Ok
    })

    //Handles can be typed if needed.
    handle "topic" (fun ctx ci (msg: Message<string>) ->
        task {
            let logger = ctx.GetLogger()
            logger.LogInformation("got string message {message} from client with Socket Id: {socketId}", msg, ci.SocketId)
            let hub = ctx.GetService<Saturn.Channels.ISocketHub>()
            do! hub.SendMessageToClients "/channel" "echo" (sprintf "payload was: %s" msg.Payload)
            return ()
        }
    )

    //Handles can specifiy it's own payload type - different topic in one channel may have different payloads
    handle "othertopic" (fun ctx ci (msg: Message<int>) ->
        task {
            let logger = ctx.GetLogger()
            logger.LogInformation("got int message {message} from client with Socket Id: {socketId}", msg, ci.SocketId)
            return ()
        }
    )
    error_handler(fun ctx ci msg ex ->
        task {
            let logger = ctx.GetLogger()
            logger.LogError(ex.Message);
            return ()
        }
    )
}


let app = application {
    use_router browserRouter
    url "http://localhost:8085/"
    add_channel "/channel" sampleChannel
    use_static ""
}

[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code
