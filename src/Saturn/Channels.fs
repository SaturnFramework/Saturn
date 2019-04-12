namespace Saturn

open FSharp.Control.Tasks.V2
open FSharp.Control.Websockets.TPL
open Giraffe.Serialization.Json
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Net.WebSockets
open System.Threading
open System.Threading.Tasks

module Socket = ThreadSafeWebsocket

module Channels =

    type Message<'a> = { Topic: string; Ref: string; Payload: 'a}
    type Message = Message<obj>
    type SocketId = string
    type ChannelPath = string
    type Topic = string

    type JoinResult =
        | Ok
        | Rejected of reason: string

    type IChannel =
        abstract member Join: HttpContext -> Task<JoinResult>
        abstract member HandleMessage: HttpContext * Message -> Task<unit>
        abstract member Terminate: HttpContext -> Task<unit>

    type ISocketHub =
        abstract member SendMessageToClients: ChannelPath -> Topic -> 'a -> CancellationToken -> Task<unit>
        abstract member SendMessageToClient: ChannelPath -> SocketId -> Topic -> 'a -> CancellationToken -> Task<unit>

    /// A type that wraps access to connected websockets by endpoint
    type SocketHub(serializer: IJsonSerializer) =
      let sockets = Dictionary<string, ConcurrentDictionary<string, Socket.ThreadSafeWebSocket>>()

      let sendMessage (msg: 'a Message) (socket: Socket.ThreadSafeWebSocket) (ctok: CancellationToken) = task {
        let text = serializer.SerializeToString msg
        let! result =  Socket.sendMessageAsUTF8 socket ctok text
        match result with
        | Socket.MessageResult.Ok () -> return ()
        | Error exn -> return exn.Throw()
      }

      member __.NewPath path =
        match sockets.TryGetValue path with
        | true, _path -> ()
        | false, _ -> sockets.[path] <- ConcurrentDictionary()

      member __.ConnectSocketToPath path socket =
        let id = Guid.NewGuid().ToString()
        sockets.[path].AddOrUpdate(id, socket, fun _ _ -> socket) |> ignore
        id

      member __.DisconnectSocketForPath path socketId =
        sockets.[path].TryRemove socketId |> ignore

      interface ISocketHub with
        member __.SendMessageToClients path topic payload ctok  = task {
          let msg = { Topic = topic; Ref = ""; Payload = payload }
          let tasks = [for kvp in sockets.[path] -> sendMessage msg kvp.Value ctok ]
          let! _results = Task.WhenAll tasks
          return ()
        }

        member __.SendMessageToClient path clientId topic payload ctok = task {
          match sockets.[path].TryGetValue clientId with
          | true, socket ->
            let msg = { Topic = topic; Ref = ""; Payload = payload }
            do! sendMessage msg socket ctok
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
                        logger.LogInformation("Promoted websocket request")
                        let! joinResult = channel.Join ctx
                        match joinResult with
                        | Ok ->
                            logger.LogInformation("Joined channel {path}", path)
                            let ctok = ctx.RequestAborted
                            let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                            let wrappedSocket = Socket.createFromWebSocket (Dataflow.DataflowBlockOptions()) webSocket // TODO: figure out what our datablock options should be
                            let socketId = sockets.ConnectSocketToPath path wrappedSocket

                            match! Socket.receiveMessageAsUTF8 wrappedSocket ctok with
                            | Core.Ok result ->
                              logger.LogInformation("received echo message {0}", result)
                              while wrappedSocket.State = WebSocketState.Open do
                                match! Socket.receiveMessageAsUTF8 wrappedSocket ctx.RequestAborted with
                                | Core.Ok msg when not (String.IsNullOrEmpty msg) ->
                                  logger.LogInformation("received message {0}", msg)
                                  let msg = serializer.Deserialize<Message> msg
                                  do! channel.HandleMessage(ctx, msg)
                                  ()
                                | Core.Ok _msg ->
                                  logger.LogError("Got null message, swallowing")
                                  ()
                                | Core.Error exn ->
                                  logger.LogError(exn.SourceException, "Error while receiving message")
                                  () // TODO: ?

                              do! channel.Terminate ctx
                              sockets.DisconnectSocketForPath path socketId
                              let! result =  Socket.close wrappedSocket ctok WebSocketCloseStatus.NormalClosure "Closing channel"
                              match result with
                              | Socket.MessageResult.Ok () ->
                                logger.LogInformation("Closed socket")
                                ()
                              | Socket.MessageResult.Error exn ->
                                logger.LogError(exn.SourceException, "Error while closing socket")
                                ()
                            | Core.Error exn ->
                              logger.LogError(exn.SourceException, "Error while joining channel")
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
        Join: (HttpContext -> Task<JoinResult>) option
        Handlers: Map<string, (HttpContext -> Message -> Task<unit>)>
        Terminate: (HttpContext -> Task<unit>) option
        NotFoundHandler: (HttpContext -> Message -> Task<unit>) option
        ErrorHandler: HttpContext -> Message -> Exception -> Task<unit>
    }

    type ChannelBuilder internal () =
        member __.Yield(_) : ChannelBuilderState =
            {Join = None; Handlers = Map.empty; Terminate = None; NotFoundHandler = None; ErrorHandler = fun _ _ ex -> raise ex }

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
                | None -> fun _ -> task {return ()}

            let findHandler msgTopic =
                state.Handlers.TryFind msgTopic

            let handler =
                fun (ctx: HttpContext) (msg : Message) -> task {
                    let logger = ctx.RequestServices.GetRequiredService<ILogger<IChannel>>()
                    logger.LogInformation("got message {message}", msg)
                    try
                        match findHandler msg.Topic with
                        | None ->
                            logger.LogInformation("no handler for topic {topic}", msg.Topic)
                            match state.NotFoundHandler with
                            | Some nfh ->
                                return! nfh ctx msg
                            | None ->
                              logger.LogInformation("no not found handler for topic {topic}", msg.Topic)
                              return ()
                        | Some hdl ->
                            logger.LogInformation("found handler for topic {topic}", msg.Topic)
                            return! hdl ctx msg
                    with
                    | ex ->
                        logger.LogError(ex, "error while handling message {message}", msg)
                        return! state.ErrorHandler ctx msg ex
                }



            { new IChannel with
                member __.Join(ctx) = join ctx

                member __.Terminate(ctx) = terminate ctx

                member __.HandleMessage(ctx, msg) =
                    handler ctx msg
            }

    let channel = ChannelBuilder()
