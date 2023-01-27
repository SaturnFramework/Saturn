---
title: Saturn | ChannelBuilder
layout: standard
---

# ChannelBuilder

**Namespace:** [Saturn](./saturn.html)

**Parent:** [ChannelBuilder](./saturn-channelbuilder.html)

Computation expression used to create channels - an `controller`-like abstraction over WebSockets allowing real-time, and push-based communication between server and the client The messages handled by channels should be json-encoded, in a following form: `{Topic = "my topic"; Ref = "unique-message-id"; Payload = {...} }`.

The result of the computation expression is the `IChannel` instance that can be registered in the `application` computation expression using `add_channel` operation.

**Example:**

```fsharp
let browserRouter = router {
  get "/ping" (fun next ctx -> task {
    let hub = ctx.GetService&lt;Saturn.Channels.ISocketHub>()
    match ctx.TryGetQueryStringValue "message" with
    | None ->
      do! hub.SendMessageToClients "/channel" "greeting" "hello"
    | Some message ->
      do! hub.SendMessageToClients "/channel" "greeting" (sprintf "hello, %s" message)
    return! Successful.ok (text "Pinged the clients") next ctx
   })
  }

let sampleChannel = channel {
  join (fun ctx si -> task {
    ctx.GetLogger().LogInformation("Connected! Socket Id: " + si.SocketId.ToString())
    return Ok
  })

  handle "topic" (fun ctx si msg ->
    task {
       let logger = ctx.GetLogger()
       logger.LogInformation("got message {message} from client with Socket Id: {socketId}", msg, si.SocketId)
       return ()
  })
}

let app = application {
  use_router browserRouter
  url "http://localhost:8085/"
  add_channel "/channel" sampleChannel
}
```

| Name                                | CE Custom Operation | Description                                                                                                                                                                                                                                                                                                                                                                                                                                              | Implementation Link                                                                           |
|-------------------------------------|---------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| `x.ErrorHandler(state, handler)`    | `error_handler`     | As arguments, `not_found_handler` action gets: current `HttpContext` for the request `ClientInfo` instance representing additional information about client sending `request * Message<'a>` instance representing message sent from client to the channel.                                                                                                                                                                                               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L264-264) |
| `x.Handle(state, topic, handler)`   | `handle`            | Action executed when client sends a message to the channel to the given topic. As arguments, `handle` action gets: current `HttpContext` for the request `ClientInfo` instance representing additional information about client sending `request * Message<'a> `instance representing message sent from client to the channel                                                                                                                            | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L232-232) |
| `x.Join(state, handler)`            | `join`              | Action executed when client tries to join the channel. You can either return `Ok` if channel allows join, or reject it with `Rejected`. Typical cases for rejection may include authorization/authentication, not being able to handle more connections or other business logic reasons. As arguments, `join` action gets: current `HttpContext` for the request `ClientInfo` instance representing additional information about client sending request. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L222-222) |
| `x.NotFoundHandler(state, handler)` | `not_found_handler` | Action executed when clients sends a message to the topic for which `handle` was not registered. As arguments, `not_found_handler` action gets: current `HttpContext` for the request `ClientInfo` instance representing additional information about client sending `request * Message<'a>` instance representing message sent from client to the channel.                                                                                              | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L255-255) |
| `x.Run(state)`                      |                     |                                                                                                                                                                                                                                                                                                                                                                                                                                                          | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L267-267) |
| `x.Terminate(state, handler)`       | `terminate`         | Action executed when client disconnects from the channel. As arguments, `join` action gets: current `HttpContext` for the request `ClientInfo` instance representing additional information about client sending request.                                                                                                                                                                                                                                | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L245-245) |
| `x.Yield(arg1)`                     |                     |                                                                                                                                                                                                                                                                                                                                                                                                                                                          | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L210-210) |
