namespace Saturn

open FSharp.Control.Tasks.V2
open FSharp.Control.Websockets
open Giraffe.Serialization.Json
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks

module Socket = ThreadSafeWebSocket

module Channels =

    type Message<'a> = { Topic: string; Ref: string; Payload: 'a}
    type Message = Message<obj>
    type SocketId = Guid
    type SocketInfo = { SocketId: SocketId }
        with
            static member New socketId =
                { SocketId = socketId }
    type ChannelPath = string
    type Topic = string

    type JoinResult =
        | Ok
        | Rejected of reason: string

    type IChannel =
        abstract member Join: HttpContext * SocketInfo -> Task<JoinResult>
        abstract member HandleMessage: HttpContext * SocketInfo * Message -> Task<unit>
        abstract member Terminate: HttpContext * SocketInfo -> Task<unit>

    type ISocketHub =
        abstract member SendMessageToClients: ChannelPath -> Topic -> 'a -> Task<unit>
        abstract member SendMessageToClient: ChannelPath -> SocketId -> Topic -> 'a -> Task<unit>

    /// A type that wraps access to connected websockets by endpoint
    type SocketHub(serializer: IJsonSerializer) =
      let sockets = Dictionary<ChannelPath, ConcurrentDictionary<SocketId, Socket.ThreadSafeWebSocket>>()

      let sendMessage (msg: 'a Message) (socket: Socket.ThreadSafeWebSocket) = task {
        let text = serializer.SerializeToString msg
        let! result =  Socket.sendMessageAsUTF8 socket text
        match result with
        | Result.Ok () -> return ()
        | Error exn -> return exn.Throw()
      }

      member __.NewPath path =
        match sockets.TryGetValue path with
        | true, _path -> ()
        | false, _ -> sockets.[path] <- ConcurrentDictionary()

      member __.ConnectSocketToPath path id socket =
        sockets.[path].AddOrUpdate(id, socket, fun _ _ -> socket) |> ignore
        id

      member __.DisconnectSocketForPath path socketId =
        sockets.[path].TryRemove socketId |> ignore

      interface ISocketHub with
        member __.SendMessageToClients path topic payload  = task {
          let msg = { Topic = topic; Ref = ""; Payload = payload }
          let tasks = [for kvp in sockets.[path] -> sendMessage msg kvp.Value ]
          let! _results = Task.WhenAll tasks
          return ()
        }

        member __.SendMessageToClient path clientId topic payload = task {
          match sockets.[path].TryGetValue clientId with
          | true, socket ->
            let msg = { Topic = topic; Ref = ""; Payload = payload }
            do! sendMessage msg socket
          | _ -> ()
        }

    type SocketMiddleware(next : RequestDelegate, serializer: IJsonSerializer, path: string, channel: IChannel, sockets: SocketHub, logger: ILogger<SocketMiddleware>) =
        do sockets.NewPath path

        member __.Invoke(ctx : HttpContext) =
            task {
                if ctx.Request.Path = PathString(path) then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let logger = ctx.RequestServices.GetRequiredService<ILogger<SocketMiddleware>>()
                        logger.LogTrace("Promoted websocket request")
                        let socketId =  Guid.NewGuid()
                        let socketInfo = SocketInfo.New socketId
                        let! joinResult = channel.Join(ctx, socketInfo)
                        match joinResult with
                        | Ok ->
                            logger.LogTrace("Joined channel {path}", path)
                            let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                            let wrappedSocket = Socket.createFromWebSocket webSocket
                            let socketId = sockets.ConnectSocketToPath path socketId wrappedSocket

                            while wrappedSocket.State = WebSocketState.Open do
                              match! Socket.receiveMessageAsUTF8 wrappedSocket with
                              | Result.Ok (WebSocket.ReceiveUTF8Result.String "") | Result.Ok (WebSocket.ReceiveUTF8Result.Closed(_)) ->
                                ()
                              | Result.Ok (WebSocket.ReceiveUTF8Result.String msg) ->
                                logger.LogTrace("received message {0}", msg)
                                try
                                  let msg = serializer.Deserialize<Message> msg
                                  do! channel.HandleMessage(ctx, socketInfo, msg)
                                with
                                | ex ->
                                  // typically a deserialization error, swallow
                                  logger.LogTrace(ex, "got message that was unable to be deserialized into a 'Message'")
                                ()
                              | Error exn ->
                                logger.LogError(exn.SourceException, "Error while receiving message")
                                () // TODO: ?

                            do! channel.Terminate (ctx, socketInfo)
                            sockets.DisconnectSocketForPath path socketId
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
module ChannelBuilder =
    open Channels

    type ChannelBuilderState = {
        Join: (HttpContext -> SocketInfo -> Task<JoinResult>) option
        Handlers: Map<string, (HttpContext-> SocketInfo -> Message -> Task<unit>)>
        Terminate: (HttpContext -> SocketInfo -> Task<unit>) option
        NotFoundHandler: (HttpContext -> SocketInfo -> Message -> Task<unit>) option
        ErrorHandler: HttpContext -> SocketInfo -> Message -> Exception -> Task<unit>
    }

    type ChannelBuilder internal () =
        member __.Yield(_) : ChannelBuilderState =
            {Join = None; Handlers = Map.empty; Terminate = None; NotFoundHandler = None; ErrorHandler = fun _ _ _ ex -> raise ex }

        [<CustomOperation("join")>]
        member __.Join(state, handler) =
            {state with Join= Some handler}

        [<CustomOperation("handle")>]
        member __.Handle(state, topic, handler) =
            {state with Handlers=state.Handlers.Add(topic, handler)}

        [<CustomOperation("terminate")>]
        member __.Terminate(state, handler) =
            {state with Terminate= Some handler}

        [<CustomOperation("not_found_handler")>]
        member __.NotFoundHandler(state : ChannelBuilderState, handler) =
            {state with NotFoundHandler= Some handler}

        [<CustomOperation("error_handler")>]
        member __.ErrorHandler(state : ChannelBuilderState, handler) =
            {state with ErrorHandler= handler}

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
                fun (ctx: HttpContext) (si: SocketInfo) (msg : Message) -> task {
                    let logger = ctx.RequestServices.GetRequiredService<ILogger<IChannel>>()
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
                            return! hdl ctx si msg
                    with
                    | ex ->
                        logger.LogError(ex, "error while handling message {message}", msg)
                        return! state.ErrorHandler ctx si msg ex
                }



            { new IChannel with
                member __.Join(ctx, si) = join ctx si

                member __.Terminate(ctx, si) = terminate ctx si

                member __.HandleMessage(ctx, si, msg) =
                    handler ctx si msg
            }

    let channel = ChannelBuilder()
