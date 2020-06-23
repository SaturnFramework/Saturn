namespace Saturn

open FSharp.Control.Tasks.V2
open FSharp.Control.Websockets
open Giraffe.Serialization.Json
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Collections.Concurrent
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks

module Socket = ThreadSafeWebSocket

module Channels =

    ///Url (relative to root application url) on which channel is hosted. Type alias for `string`
    type ChannelPath = string

    ///Topic of the channel. Type alias for `string`
    type Topic = string

    /// Types representing channels message.
    /// It always includes topic, reference id of the message (random GUID), and payload object.
    type Message<'a> = { Topic: Topic; Ref: string; Payload: 'a}

    ///Socket Id. Type alias for `Guid`
    type SocketId = Guid

    ///Type representing information about client that has executed some channel action
    ///It's passed as an argument in channel actions (`join`, `handle`, `terminate`)
    type ClientInfo = { SocketId: SocketId; ChannelPath: ChannelPath }
        with
            static member New channelPath socketId =
                { SocketId = socketId; ChannelPath = channelPath  }

    ///Type representing result of `join` action. It can be either succesful (`Ok`) or you can reject client connection (`Rejected`)
    type JoinResult =
        | Ok
        | Rejected of reason: string

    /// Interface of the internal representation of the channel.
    /// Shouldn't be used manually, you get its instance from the `channel` Computation Expression
    type IChannel =
        abstract member Join: HttpContext * ClientInfo -> Task<JoinResult>
        abstract member HandleMessage: HttpContext * ClientInfo * IJsonSerializer * string -> Task<unit>
        abstract member Terminate: HttpContext * ClientInfo -> Task<unit>

    /// Interface representing server side Socket Hub, giving you ability to brodcast messages (either to particular socket or to all sockets).
    /// You can get instance of it with `ctx.GetService&lt;Saturn.Channels.ISocketHub>()` from any place that has access to HttpContext instance (`controller` actions, `channel` actions, normal `HttpHandler`)
    type ISocketHub =
        abstract member SendMessageToClients: ChannelPath -> Topic -> 'a -> Task<unit>
        abstract member SendMessageToClient: ClientInfo -> Topic -> 'a -> Task<unit>
        abstract member SendMessageToClientsFilter: (ClientInfo -> bool) -> Topic -> 'a -> Task<unit>

    /// A type that wraps access to connected websockets by endpoint
    type SocketHub(serializer: IJsonSerializer) =
      let sockets = ConcurrentDictionary<ClientInfo, Socket.ThreadSafeWebSocket>()

      let sendMessage (msg: 'a Message) (socket: Socket.ThreadSafeWebSocket) = task {
        let text = serializer.SerializeToString msg
        let! result =  Socket.sendMessageAsUTF8 socket text
        match result with
        | Result.Ok () -> return ()
        | Error exn -> return exn.Throw()
      }

      member __.ConnectSocketToPath path clientId socket =
        let ci = {SocketId = clientId; ChannelPath = path}
        sockets.AddOrUpdate(ci, socket, fun _ _ -> socket) |> ignore
        ci

      member __.DisconnectSocketForPath path clientId =
        let ci = {SocketId = clientId; ChannelPath = path}
        sockets.TryRemove ci |> ignore

      interface ISocketHub with
        member __.SendMessageToClientsFilter(predicate: ClientInfo -> bool) (topic: Topic) (payload: 'a): Task<unit> = task {
          let msg = { Topic = topic; Ref = ""; Payload = payload }
          let tasks =
            sockets
            |> Seq.filter (fun n -> predicate n.Key)
            |> Seq.map (fun n -> sendMessage msg n.Value)

          let! _results = Task.WhenAll tasks
          return ()
          }

        member __.SendMessageToClients path topic payload  = task {
          let msg = { Topic = topic; Ref = ""; Payload = payload }
          let tasks =
            sockets
            |> Seq.filter (fun n -> n.Key.ChannelPath = path)
            |> Seq.map (fun n -> sendMessage msg n.Value)

          let! _results = Task.WhenAll tasks
          return ()
        }

        member __.SendMessageToClient clientInfo topic payload = task {
          match sockets.TryGetValue clientInfo with
          | true, socket ->
            let msg = { Topic = topic; Ref = ""; Payload = payload }
            do! sendMessage msg socket
          | _ -> ()
        }

    type SocketMiddleware(next : RequestDelegate, serializer: IJsonSerializer, path: string, channel: IChannel, sockets: SocketHub, logger: ILogger<SocketMiddleware>) =
        member __.Invoke(ctx : HttpContext) =
            task {
                if ctx.Request.Path = PathString(path) then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let logger = ctx.RequestServices.GetRequiredService<ILogger<SocketMiddleware>>()
                        logger.LogTrace("Promoted websocket request")
                        let socketId =  Guid.NewGuid()
                        let clientInfo = ClientInfo.New path socketId
                        let! joinResult = channel.Join(ctx, clientInfo)
                        match joinResult with
                        | Ok ->
                            logger.LogTrace("Joined channel {path}", path)
                            let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                            let wrappedSocket = Socket.createFromWebSocket webSocket
                            let clientInfo = sockets.ConnectSocketToPath path socketId wrappedSocket

                            while wrappedSocket.State = WebSocketState.Open do
                              match! Socket.receiveMessageAsUTF8 wrappedSocket with
                              | Result.Ok (WebSocket.ReceiveUTF8Result.String "") | Result.Ok (WebSocket.ReceiveUTF8Result.Closed(_)) ->
                                ()
                              | Result.Ok (WebSocket.ReceiveUTF8Result.String msg) ->
                                logger.LogTrace("received message {0}", msg)
                                try
                                  do! channel.HandleMessage(ctx, clientInfo, serializer, msg)
                                with
                                | ex ->
                                  // typically a deserialization error, swallow
                                  logger.LogTrace(ex, "got message that was unable to be deserialized into a 'Message'")
                                ()
                              | Error exn ->
                                logger.LogError(exn.SourceException, "Error while receiving message")
                                () // TODO: ?

                            do! channel.Terminate (ctx, clientInfo)
                            sockets.DisconnectSocketForPath path clientInfo.SocketId
                            let! result =  Socket.close wrappedSocket WebSocketCloseStatus.NormalClosure "Closing channel"
                            match result with
                            | Result.Ok () ->
                              logger.LogTrace("Closed socket")
                              ()
                            | Error exn ->
                              logger.LogError(exn.SourceException, "Error while closing socket")
                              ()
                        | Rejected msg ->
                            ctx.Response.StatusCode <- 400
                            do! ctx.Response.WriteAsync msg
                    | false ->
                      ctx.Response.StatusCode <- 400
                else do! next.Invoke(ctx) |> (Async.AwaitIAsyncResult >> Async.Ignore)
            } :> Task


[<AutoOpen>]
///Module with `channel` computation expression
module ChannelBuilder =
    open Channels

    ///Type representing internal state of the `channel` computation expression
    type ChannelBuilderState = {
        Join: (HttpContext -> ClientInfo -> Task<JoinResult>) option
        Handlers: Map<string, (IJsonSerializer -> HttpContext-> ClientInfo -> string -> Task<unit>)>
        Terminate: (HttpContext -> ClientInfo -> Task<unit>) option
        NotFoundHandler: (HttpContext -> ClientInfo -> Message<obj> -> Task<unit>) option
        ErrorHandler: HttpContext -> ClientInfo -> Message<obj> -> Exception -> Task<unit>
    }

    ///Computation expression used to create channels - an `controller`-like abstraction over WebSockets allowing real-time, and push-based communication between server and the client
    /// The messages handled by channels should be json-encoded, in a following form: `{Topic = "my topic"; Ref = "unique-message-id"; Payload = {...} }`
    ///
    ///The result of the computation expression is the `IChannel` instance that can be registered in the `application` computation expression using `add_channel` operation.
    ///
    ///**Example:**
    ///
    /// ```fsharp
    ///
    /// let browserRouter = router {
    ///   get "/ping" (fun next ctx -> task {
    ///     let hub = ctx.GetService&lt;Saturn.Channels.ISocketHub>()
    ///     match ctx.TryGetQueryStringValue "message" with
    ///     | None ->
    ///       do! hub.SendMessageToClients "/channel" "greeting" "hello"
    ///     | Some message ->
    ///       do! hub.SendMessageToClients "/channel" "greeting" (sprintf "hello, %s" message)
    ///     return! Successful.ok (text "Pinged the clients") next ctx
    ///    })
    ///   }
    ///
    /// let sampleChannel = channel {
    ///   join (fun ctx si -> task {
    ///     ctx.GetLogger().LogInformation("Connected! Socket Id: " + si.SocketId.ToString())
    ///     return Ok
    ///   })
    ///
    ///   handle "topic" (fun ctx si msg ->
    ///     task {
    ///        let logger = ctx.GetLogger()
    ///        logger.LogInformation("got message {message} from client with Socket Id: {socketId}", msg, si.SocketId)
    ///        return ()
    ///   })
    /// }
    ///
    /// let app = application {
    ///   use_router browserRouter
    ///   url "http://localhost:8085/"
    ///   add_channel "/channel" sampleChannel
    /// }
    /// ```
    type ChannelBuilder internal () =
        member __.Yield(_) : ChannelBuilderState =
            {Join = None; Handlers = Map.empty; Terminate = None; NotFoundHandler = None; ErrorHandler = fun _ _ _ ex -> raise ex }

        [<CustomOperation("join")>]
        ///Action executed when client tries to join the channel.
        ///You can either return `Ok` if channel allows join, or reject it with `Rejected`
        ///Typical cases for rejection may include authorization/authentication,
        ///not being able to handle more connections or other business logic reasons.
        ///
        /// As arguments, `join` action gets:
        /// *  current `HttpContext` for the request
        /// * `ClientInfo` instance representing additional information about client sending request
        member __.Join(state, handler) =
            {state with Join= Some handler}

        [<CustomOperation("handle")>]
        ///Action executed when client sends a message to the channel to the given topic.
        ///
        /// As arguments, `handle` action gets:
        /// *  current `HttpContext` for the request
        /// * `ClientInfo` instance representing additional information about client sending request
        /// * `Message<'a>` instance representing message sent from client to the channel
        member __.Handle<'a>(state, topic, (handler : HttpContext -> ClientInfo -> Message<'a> -> Task<unit>)) =
            let objHandler = fun (serializer: IJsonSerializer) ctx ci (msg: string) ->
              let nmsg = serializer.Deserialize<Message<'a>> msg
              handler ctx ci nmsg

            {state with Handlers=state.Handlers.Add(topic, objHandler)}

        [<CustomOperation("terminate")>]
        ///Action executed when client disconnects from the channel
        ///
        /// As arguments, `join` action gets:
        /// *  current `HttpContext` for the request
        /// * `ClientInfo` instance representing additional information about client sending request
        member __.Terminate(state, handler) =
            {state with Terminate= Some handler}

        [<CustomOperation("not_found_handler")>]
        ///Action executed when clients sends a message to the topic for which `handle` was not registered
        ///
        /// As arguments, `not_found_handler` action gets:
        /// *  current `HttpContext` for the request
        /// * `ClientInfo` instance representing additional information about client sending request
        /// * `Message<'a>` instance representing message sent from client to the channel
        member __.NotFoundHandler(state, handler) =
            {state with ChannelBuilderState.NotFoundHandler= Some handler}

        [<CustomOperation("error_handler")>]
        ///Action executed when unhandled exception happens in the
        /// As arguments, `not_found_handler` action gets:
        /// *  current `HttpContext` for the request
        /// * `ClientInfo` instance representing additional information about client sending request
        /// * `Message<'a>` instance representing message sent from client to the channel
        member __.ErrorHandler(state, handler) =
            {state with ChannelBuilderState.ErrorHandler= handler}

        member __.Run(state: ChannelBuilderState) : IChannel =
            if state.Join.IsNone then failwith "Join is required operation for any channel. Please use `join` operation in your `channel` CE to define it."

            let join = state.Join.Value

            let terminate =
                match state.Terminate with
                | Some v -> v
                | None -> fun _ _ -> task {return ()}

            let findHandler msgTopic =
                state.Handlers.TryFind msgTopic

            let handler =
                fun (serializer: IJsonSerializer) (ctx: HttpContext) (si: ClientInfo) (rawMsg : string) -> task {
                    let logger = ctx.RequestServices.GetRequiredService<ILogger<IChannel>>()
                    let msg = serializer.Deserialize<Message<obj>> rawMsg
                    logger.LogInformation("got message {message}", msg)
                    try
                        match findHandler msg.Topic with
                        | None ->
                            logger.LogInformation("no handler for topic {topic}", msg.Topic)
                            match state.NotFoundHandler with
                            | Some nfh ->
                                return! nfh ctx si msg
                            | None ->
                              logger.LogInformation("no not found handler for topic {topic}", msg.Topic)
                              return ()
                        | Some hdl ->
                            logger.LogInformation("found handler for topic {topic}", msg.Topic)
                            return! hdl serializer ctx si rawMsg
                    with
                    | ex ->
                        logger.LogError(ex, "error while handling message {message}", msg)
                        return! state.ErrorHandler ctx si msg ex
                }



            { new IChannel with
                member __.Join(ctx, si) = join ctx si

                member __.Terminate(ctx, si) = terminate ctx si

                member __.HandleMessage(ctx, si, serializer, msg) =
                    handler serializer ctx si msg
            }

    ///Computation expression used to create channels
    let channel = ChannelBuilder()
