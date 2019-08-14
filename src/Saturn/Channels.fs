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
    type ChannelPath = string
    type Topic = string
    type GroupName = string


    type JoinResult =
        | Ok
        | Rejected of reason: string

    type ISocketHub =
        abstract member SendMessageToClients: ChannelPath -> Topic -> 'a -> Task<unit>
        abstract member SendMessageToClient: ChannelPath -> SocketId -> Topic -> 'a -> Task<unit>
        abstract member SendMessageToGroup: ChannelPath -> GroupName -> 'a -> Task<unit>
        abstract member AddConnectionToGroup: ChannelPath -> SocketId -> GroupName -> unit
        abstract member RemoveConnectionFromGroup: ChannelPath -> SocketId -> GroupName -> unit

    type IChannel =
        abstract member Join: HttpContext * SocketId -> Task<JoinResult>
        abstract member HandleMessage: HttpContext * Message -> Task<unit>
        abstract member Terminate: HttpContext -> Task<unit>
        abstract member OnConnected: HttpContext * ISocketHub * SocketId -> Task<unit>
        abstract member OnDisconnected: HttpContext * ISocketHub * SocketId -> Task<unit>

    /// A type that wraps access to connected websockets by endpoint
    type SocketHub(serializer: IJsonSerializer) =
      let sockets = Dictionary<ChannelPath, ConcurrentDictionary<SocketId, Socket.ThreadSafeWebSocket>>()
      let groups = Dictionary<ChannelPath, ConcurrentDictionary<GroupName, ConcurrentDictionary<SocketId, SocketId>>>()

      let sendMessage (msg: 'a Message) (socket: Socket.ThreadSafeWebSocket) = task {
        let text = serializer.SerializeToString msg
        let! result =  Socket.sendMessageAsUTF8 socket text
        match result with
        | Result.Ok () -> return ()
        | Error exn -> return exn.Throw()
      }

      let addSocketToGroup socketId (group: ConcurrentDictionary<SocketId, SocketId>) =
        group.AddOrUpdate(socketId, socketId, Func<SocketId, SocketId, SocketId>(fun _ _ -> socketId)) |> ignore
        group

      let remove channelPath socketId group =
        match groups.[channelPath].TryGetValue(group) with
        | true, connections ->
          match connections.TryRemove(socketId) with
          | true, _ when connections.IsEmpty ->
            let groupToRemove = new KeyValuePair<string, ConcurrentDictionary<_,_>>(group, ConcurrentDictionary<_, _>())
            (groups.[channelPath] :> ICollection<KeyValuePair<string, ConcurrentDictionary<_, _>>>).Remove(groupToRemove) |> ignore
          | _ -> ()
        | _ -> ()

      let removeDisconnectedSocket channelPath socketId =
        let groupNames = groups.[channelPath] |> Seq.filter (fun x -> x.Value.Keys.Contains(socketId)) |> Seq.map (fun x -> x.Key)
        for group in groupNames do
          remove channelPath socketId group

      let createOrUpdateGroupWithConnection channelPath group socketId =
        let groups = groups.[channelPath]
        let adder = Func<GroupName, ConcurrentDictionary<SocketId, SocketId>>(fun key -> addSocketToGroup socketId (new ConcurrentDictionary<_, _>()))
        let updater = Func<GroupName, ConcurrentDictionary<_, _>, ConcurrentDictionary<_, _>>(fun key collection ->
          addSocketToGroup socketId collection |> ignore
          collection)
        groups.AddOrUpdate(group, adder, updater)

      member __.NewPath path =
        match sockets.TryGetValue path with
        | true, _path -> ()
        | false, _ -> sockets.[path] <- ConcurrentDictionary()
        match groups.TryGetValue path with
        | true, _path -> ()
        | false, _ -> groups.[path] <- ConcurrentDictionary()

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

        member __.SendMessageToGroup (path: ChannelPath) group payload = task {
          match groups.[path].TryGetValue group with
          | true, members ->
            let msg = { Topic = ""; Ref = ""; Payload = payload }
            let tasks = [
                for memb in members do
                  match sockets.[path].TryGetValue memb.Key with
                  | true, socket -> yield sendMessage msg socket
                  | _ -> () ]
            let! _results = Task.WhenAll tasks
            return ()
          | _ -> return ()
        }

        member __.AddConnectionToGroup (path: ChannelPath) (socketId: SocketId) (group: GroupName) =
          let bag = ConcurrentDictionary<_, _>([KeyValuePair(socketId, socketId)])
          let updater = Func<GroupName, ConcurrentDictionary<SocketId, SocketId>, ConcurrentDictionary<SocketId, SocketId>>(fun group bag ->
            let _ = bag.TryAdd(socketId, socketId)
            bag)
          let bag = groups.[path].AddOrUpdate(group, bag, updater)
          ()

        member __.RemoveConnectionFromGroup (path: ChannelPath) (socketId: SocketId) (group: GroupName) =
          remove path socketId group



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
                        let! joinResult = channel.Join(ctx, socketId)
                        match joinResult with
                        | Ok ->
                            logger.LogTrace("Joined channel {path}", path)
                            let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                            let wrappedSocket = Socket.createFromWebSocket webSocket
                            let socketId = sockets.ConnectSocketToPath path socketId wrappedSocket
                            do! channel.OnConnected(ctx, sockets, socketId)
                            while wrappedSocket.State = WebSocketState.Open do
                              match! Socket.receiveMessageAsUTF8 wrappedSocket with
                              | Result.Ok (WebSocket.ReceiveUTF8Result.String "") | Result.Ok (WebSocket.ReceiveUTF8Result.Closed(_)) ->
                                ()
                              | Result.Ok (WebSocket.ReceiveUTF8Result.String msg) ->
                                logger.LogTrace("received message {0}", msg)
                                try
                                  let msg = serializer.Deserialize<Message> msg
                                  do! channel.HandleMessage(ctx, msg)
                                with
                                | ex ->
                                  // typically a deserialization error, swallow
                                  logger.LogTrace(ex, "got message that was unable to be deserialized into a 'Message'")
                                ()
                              | Error exn ->
                                logger.LogError(exn.SourceException, "Error while receiving message")
                                () // TODO: ?

                            do! channel.Terminate ctx
                            sockets.DisconnectSocketForPath path socketId
                            let! result =  Socket.close wrappedSocket WebSocketCloseStatus.NormalClosure "Closing channel"
                            match result with
                            | Result.Ok () ->
                              logger.LogTrace("Closed socket")
                              do! channel.OnDisconnected(ctx, sockets, socketId)
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
        Join: (HttpContext -> SocketId -> Task<JoinResult>) option
        Handlers: Map<string, (HttpContext -> Message -> Task<unit>)>
        Terminate: (HttpContext -> Task<unit>) option
        OnConnected: (HttpContext -> ISocketHub -> SocketId -> Task<unit>) option
        OnDisconnected: (HttpContext -> ISocketHub -> SocketId -> Task<unit>) option
        NotFoundHandler: (HttpContext -> Message -> Task<unit>) option
        ErrorHandler: HttpContext -> Message -> Exception -> Task<unit>
    }

    type ChannelBuilder internal () =
        member __.Yield(_) : ChannelBuilderState =
            {Join = None; Handlers = Map.empty; Terminate = None; OnConnected = None; OnDisconnected = None; NotFoundHandler = None; ErrorHandler = fun _ _ ex -> raise ex }

        [<CustomOperation("join")>]
        member __.Join(state, handler) =
            {state with Join= Some handler}

        [<CustomOperation("handle")>]
        member __.Handle(state, topic, handler) =
            {state with Handlers=state.Handlers.Add(topic, handler)}

        [<CustomOperation("terminate")>]
        member __.Terminate(state, handler) =
            {state with Terminate= Some handler}

        [<CustomOperation("on_connected")>]
        member __.OnConnected(state, handler) =
            {state with OnConnected = Some handler}

        [<CustomOperation("on_disconnected")>]
        member __.OnDisconnected(state, handler) =
            {state with OnDisconnected = Some handler}

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

            let onConnected =
                state.OnConnected
                |> Option.defaultValue (fun _ _ _ -> task {return ()})

            let onDisconnected =
                state.OnDisconnected
                |> Option.defaultValue (fun _ _ _ -> task { return ()})

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
                member __.Join(ctx,id) = join ctx id

                member __.Terminate(ctx) = terminate ctx

                member __.HandleMessage(ctx, msg) =
                    handler ctx msg

                member __.OnConnected(ctx, hub, connectionId) = onConnected ctx hub connectionId

                member __.OnDisconnected(ctx, hub, connectionId) = onDisconnected ctx hub connectionId
            }

    let channel = ChannelBuilder()
