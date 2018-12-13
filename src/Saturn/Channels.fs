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

module Channels =
    let internal sockets = Dictionary<string, ConcurrentDictionary<string, WebSocket>>()

    type Message = {Topic: string; Ref: string; Payload: obj}

    type JoinResult =
        | Ok
        | Rejected of reason: string

    type IChannel =
        abstract member Join: HttpContext -> Task<JoinResult>
        abstract member HandleMessage: HttpContext * WebSocketReceiveResult * Message -> Task<unit>
        abstract member Terminate: HttpContext -> Task<unit>

    type SocketMiddleware(next : RequestDelegate, serializer: IJsonSerializer, path: string, channel: IChannel) =
        do sockets.Add(path, ConcurrentDictionary())

        member __.Invoke(ctx : HttpContext) =
            task {
                if ctx.Request.Path = PathString(path) then
                    match ctx.WebSockets.IsWebSocketRequest with
                    | true ->
                        let! joinResult = channel.Join ctx
                        match joinResult with
                        | Ok ->
                            let! webSocket = ctx.WebSockets.AcceptWebSocketAsync()
                            let guid = Guid.NewGuid().ToString()
                            sockets.[path].AddOrUpdate (guid, webSocket, fun _ _ -> webSocket) |> ignore

                            let buffer : byte [] = Array.zeroCreate 4096
                            let! echo = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)

                            while not echo.CloseStatus.HasValue do
                                let! result =
                                    webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
                                let msg = serializer.Deserialize<Message> buffer
                                do! channel.HandleMessage(ctx, result, msg)
                                ()

                            do! channel.Terminate ctx
                            sockets.[path].TryRemove(guid) |> ignore
                            do! webSocket.CloseAsync(echo.CloseStatus.Value, echo.CloseStatusDescription, CancellationToken.None)
                        | Rejected msg ->
                            ctx.Response.StatusCode <- 400
                            do! ctx.Response.WriteAsync msg
                    | false -> ctx.Response.StatusCode <- 400
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