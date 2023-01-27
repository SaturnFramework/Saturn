---
title: Saturn | ChannelBuilder
layout: standard
---

# ChannelBuilder

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Saturn](./saturn.html)

Module with `channel` computation expression.

**Declared Types**

* **Type:** [ChannelBuilder](./saturn-channelbuilder-channelbuilder.html)

  **Description:** Computation expression used to create channels - an `controller`-like abstraction over WebSockets allowing real-time, and push-based communication between server and the client The messages handled by channels should be json-encoded, in a following form: `{Topic = "my topic"; Ref = "unique-message-id"; Payload = {...} }`.

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

---

* **Type:** [ChannelBuilderState](./saturn-channelbuilder-channelbuilderstate.html)

  **Description:** Type representing internal state of the `channel` computation expression.

---

**Values and Functions**

| Name      | Description                                     | Implementation Link                                                                           |
|-----------|-------------------------------------------------|-----------------------------------------------------------------------------------------------|
| `channel` | Computation expression used to create channels. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Channels.fs#L316-316) |
