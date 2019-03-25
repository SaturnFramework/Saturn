namespace Saturn

open System
open System.Threading
open System.Threading.Tasks
open System.Net.WebSockets
open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System.Collections.Generic
open System.Collections.Concurrent
open Giraffe.Serialization.Json
open System.IO
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Http.Features
open Microsoft.AspNetCore.Http.Features

module Channels =
    open System.Collections.Concurrent

    type Message<'a> = { Topic: string; Ref: string; Payload: 'a}
    type Message = Message<obj>
    type SocketId = string
    type ChannelPath = string

    type JoinResult =
        | Ok
        | Rejected of reason: string

    type IChannel =
        abstract member Join: HttpContext -> Task<JoinResult>
        abstract member HandleMessage: HttpContext * WebSocketReceiveResult * Message -> Task<unit>
        abstract member Terminate: HttpContext -> Task<unit>

    type ISocketHub =
        abstract member SendMessageToClients: ChannelPath -> Message<'a> -> Task<unit>
        abstract member SendMessageToClient: ChannelPath -> SocketId -> Message<'a> -> Task<unit>

    /// A type that wraps access to connected websockets by endpoint
    type SocketHub(serializer: IJsonSerializer) =
      let sockets = Dictionary<string, ConcurrentDictionary<string, WebSocket>>()

      let sendMessage (msg: 'a Message) (socket: WebSocket) = task {
        let bytes = serializer.SerializeToBytes msg
        let buf = ArraySegment(bytes)
        do! socket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None)
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
        member __.SendMessageToClients path (msg: Message<'a>) = task {
          let tasks = [for kvp in sockets.[path] -> sendMessage msg kvp.Value ]
          let! _results = Task.WhenAll tasks
          return ()
        }

        member __.SendMessageToClient path clientId (msg: Message<'a>) = task {
          match sockets.[path].TryGetValue clientId with
          | true, socket -> do! sendMessage msg socket
          | _ -> ()
        }

    type SocketMiddleware(next : RequestDelegate, serializer: IJsonSerializer, path: string, channel: IChannel, sockets: SocketHub, logger: ILogger<SocketMiddleware>) =
        do sockets.NewPath path

        /// **Description**
        ///
        /// (16 * 1024) = 16384
        /// https://referencesource.microsoft.com/#System/net/System/Net/WebSockets/WebSocketHelpers.cs,285b8b64a4da6851
        ///
        /// **Output Type**
        ///   * `int`
        [<Literal>]
        let defaultBufferSize : int = 16384 // (16 * 1024)


        let receiveMessage cancellationToken bufferSize messageType (writeableStream : IO.Stream) (socket : WebSocket) = task {
            let buffer = new ArraySegment<Byte>( Array.create (bufferSize) Byte.MinValue)
            let mutable moreToRead = false
            let mutable res = None
            while moreToRead do
                let! result  = socket.ReceiveAsync(buffer,cancellationToken)
                res <- Some result
                match result with
                | result when result.MessageType = WebSocketMessageType.Close || socket.State = WebSocketState.CloseReceived ->
                    // printfn "Close received! %A - %A" socket.CloseStatus socket.CloseStatusDescription
                    do! socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,  "Close received", cancellationToken)
                | result ->
                    // printfn "result.MessageType -> %A" result.MessageType
                    if result.MessageType <> messageType then
                        failwithf "Invalid message type received %A, expected %A" result.MessageType messageType
                    do! writeableStream.WriteAsync(buffer.Array, buffer.Offset, result.Count)
                    if result.EndOfMessage then
                        moreToRead <- false
            return res.Value
        }


        let receiveMessageAsUTF8 cancellationToken socket = task {
            use stream =  new IO.MemoryStream()
            let! res = receiveMessage cancellationToken defaultBufferSize WebSocketMessageType.Text stream socket
            stream.Seek(0L,IO.SeekOrigin.Begin) |> ignore
            let cnt =
                stream.ToArray()
                |> Text.Encoding.UTF8.GetString
                |> fun s -> s.TrimEnd(char 0)
            return res,cnt
        }

        member __.Invoke(ctx : HttpContext) =
            task {
                if ctx.Request.Path = PathString(path) then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let! joinResult = channel.Join ctx
                        match joinResult with
                        | Ok ->
                            let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                            let socketId = sockets.ConnectSocketToPath path webSocket

                            let buffer : byte [] = Array.zeroCreate defaultBufferSize //It's buffer for just open message.
                            let! echo = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)

                            while not echo.CloseStatus.HasValue do
                                let! (result, msg) = receiveMessageAsUTF8 CancellationToken.None webSocket
                                let msg = serializer.Deserialize<Message> msg
                                do! channel.HandleMessage(ctx, result, msg)
                                ()

                            do! channel.Terminate ctx
                            sockets.DisconnectSocketForPath path socketId
                            do! webSocket.CloseAsync(echo.CloseStatus.Value, echo.CloseStatusDescription, CancellationToken.None)
                        | Rejected msg ->
                            ctx.Response.StatusCode <- 400
                            do! ctx.Response.WriteAsync msg
                    | false ->
                      logger.LogInformation("path: {0}", ctx.Request.Path)
                      logger.LogInformation("IsWebsocketRequest: {0}", ctx.WebSockets.IsWebSocketRequest)
                      let feature = ctx.Features.[typeof<IHttpWebSocketFeature>] :?> IHttpWebSocketFeature
                      if feature <> null
                      then
                        logger.LogInformation("Feature isWebsocketREquest: {0}", feature.IsWebSocketRequest)
                      else
                        logger.LogWarning("WTF no websocket feature")
                      let feature = ctx.Features.[typeof<IHttpUpgradeFeature>] :?> IHttpUpgradeFeature
                      if feature <> null
                      then
                        logger.LogInformation("Feature isUpgradeableRequest: {0}", feature.IsUpgradableRequest)
                      else
                        logger.LogWarning("WTF no upgrade feature")

                      for (KeyValue(feature, impl)) in ctx.Features do
                        printfn "%s:\t%s" feature.FullName (impl.GetType().FullName)

                      ctx.Response.StatusCode <- 400
                else do! next.Invoke(ctx) |> (Async.AwaitIAsyncResult >> Async.Ignore)
            } :> Task



[<AutoOpen>]
module ChannelBuilder =
    open Channels

    type ChannelBuilderState = {
        Join: (HttpContext -> Task<JoinResult>) option
        Handlers: Map<string, (HttpContext -> WebSocketReceiveResult -> Message -> Task<unit>)>
        Terminate: (HttpContext -> Task<unit>) option
        NotFoundHandler: (HttpContext -> WebSocketReceiveResult -> Message -> Task<unit>) option
        ErrorHandler: HttpContext -> WebSocketReceiveResult -> Message -> Exception -> Task<unit>
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
                | None -> fun _ -> task {return ()}

            let findHandler msgTopic =
                state.Handlers.TryFind msgTopic

            let handler =
                fun ctx res (msg : Message) -> task {
                    try
                        match findHandler msg.Topic with
                        | None ->
                            match state.NotFoundHandler with
                            | Some nfh ->
                                return! nfh ctx res msg
                            | None -> return ()
                        | Some hdl ->
                            return! hdl ctx res msg
                    with
                    | ex ->
                        return! state.ErrorHandler ctx res msg ex
                }



            { new IChannel with
                member __.Join(ctx) = join ctx

                member __.Terminate(ctx) = terminate ctx

                member __.HandleMessage(ctx,res,msg) =
                    handler ctx res msg
            }

    let channel = ChannelBuilder()
