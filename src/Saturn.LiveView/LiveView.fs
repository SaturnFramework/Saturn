namespace Saturn

open Channels
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open System.Threading.Tasks
open Giraffe.GiraffeViewEngine
open Elmish
open FSharp.Control.Tasks.V2

module LiveComponenet =
  type ILiveComponenet =
    abstract member InternalChannel : IChannel with get

  type LiveComponentMsg = {Event: string; ElementId: string; Data: string}
  type internal ViewUpdateMsg = {ComponentId: string; Data: string}

[<AutoOpen>]
module LiveComponentBuilder =
  open LiveComponenet

  type LiveComponenetBuilderState<'State, 'Msg> = {
    Join: (HttpContext -> ClientInfo -> Task<JoinResult>) option
    Init: (HttpContext -> ClientInfo -> (Cmd<'Msg> -> unit) -> Task<'State * Cmd<'Msg>>) option
    Update: (HttpContext -> ClientInfo -> 'Msg -> 'State -> Task<'State * Cmd<'Msg>>) option
    View: (HttpContext -> ClientInfo -> 'State -> XmlNode) option
    MessageMap: (HttpContext -> ClientInfo -> LiveComponentMsg -> 'Msg) option
  }

  type internal StateMsg<'State, 'Msg> =
    | Init of HttpContext * ClientInfo
    | SetState of 'State
    | Dispatch of Cmd<'Msg>
    | Update of 'Msg


  type LiveComponenetBuilder<'State, 'Msg> internal (componentId: string) =

    member __.Yield (_) : LiveComponenetBuilderState<'State, 'Msg> =
      {Join = None; Init = None; Update = None; View = None; MessageMap = None}

    [<CustomOperation("join")>]
    ///Action executed when client tries to join the channel.
    ///You can either return `Ok` if channel allows join, or reject it with `Rejected`
    ///Typical cases for rejection may include authorization/authentication,
    ///not being able to handle more connections or other business logic reasons.
    ///
    /// As arguments, `join` action gets:
    /// *  current `HttpContext` for the request
    /// * `ClientInfo` instance representing additional information about client sending request
    member __.Join (state, handler) : LiveComponenetBuilderState<'State, 'Msg> =
      {state with Join = handler}

    [<CustomOperation("init")>]
    ///Action executed after client succesfully join the channel. Used to set initial state of the compnent.
    ///
    /// As arguments, `init` action gets:
    /// *  current `HttpContext` for the request
    /// * `ClientInfo` instance representing additional information about client sending request
    /// * `(Cmd<'Msg> -> unit)` function that can be used to dispatch additional messages (for example used when in `init` you can subscribe to external events)
    ///
    /// Returns: `Task<'State * Cmd<'Msg>>`
    member __.Init (state, handler) : LiveComponenetBuilderState<'State, 'Msg> =
      {state with Init = handler}

    [<CustomOperation("update")>]
    ///Action executed after client performs some event in the component
    ///
    /// As arguments, `update` action gets:
    /// *  current `HttpContext` for the request
    /// * `ClientInfo` instance representing additional information about client sending request
    /// * message `'Msg` that represetns event that happened
    ///
    /// Returns: `Task<'State * Cmd<'Msg>>`
    member __.Update (state, handler) : LiveComponenetBuilderState<'State, 'Msg> =
      {state with Update = handler}

    [<CustomOperation("view")>]
    ///Function responsible for mapping current state to the view
    ///
    /// As arguments, `view` action gets:
    /// *  current `HttpContext` for the request
    /// * `ClientInfo` instance representing additional information about client sending request
    /// * current state `'State`
    ///
    /// Returns: `XmlNode` (Giraffe.ViewEngine)
    member __.View (state, handler) : LiveComponenetBuilderState<'State, 'Msg> =
      {state with View = handler}

    [<CustomOperation("message_map")>]
    ///Function responsible for mapping raw messages into component domain messages
    ///
    /// As arguments, `message_map` action gets:
    /// *  current `HttpContext` for the request
    /// * `ClientInfo` instance representing additional information about client sending request
    /// * instance of `LiveComponentMsg` representing raw message
    ///
    /// Returns: `'Msg` representing domain message
    member __.MessageMap (state, handler) : LiveComponenetBuilderState<'State, 'Msg> =
      {state with MessageMap = handler}

    member __.Run (state : LiveComponenetBuilderState<'State, 'Msg>) : ILiveComponenet =
      if state.Join.IsNone then failwith "Join is required operation for any Live Component. Please use `join` operation in your `liveComponent` CE to define it."
      if state.Init.IsNone then failwith "Init is required operation for any Live Component. Please use `init` operation in your `liveComponent` CE to define it."
      if state.View.IsNone then failwith "View is required operation for any Live Component. Please use `view` operation in your `liveComponent` CE to define it."
      if state.Update.IsNone then failwith "Update is required operation for any Live Component. Please use `update` operation in your `liveComponent` CE to define it."
      if state.MessageMap.IsNone then failwith "MessageMap is required operation for any Live Component. Please use `message_map` operation in your `liveComponent` CE to define it."


      let joinH = state.Join.Value
      let initH = state.Init.Value
      let viewH = state.View.Value
      let updateH = state.Update.Value
      let mmH = state.MessageMap.Value

      let c =
        let rec stateMP = MailboxProcessor.Start(fun inbox ->

          let rec messageLoop(state: 'State, (ctx: HttpContext), ci) = async {
            let! msg = inbox.Receive()
            let! newState, ctx, ci =
              match msg with
              | Init (ctx, ci) ->
                async { return state, ctx, ci}
              | SetState (state) ->
                async {
                  let clientHub = ctx.RequestServices.GetService<ISocketHub> ()
                  let viewTemplate = viewH ctx ci state
                  let viewStr = Giraffe.GiraffeViewEngine.renderHtmlDocument viewTemplate
                  let viewMsg = {ComponentId = componentId; Data = viewStr}
                  do! clientHub.SendMessageToClient ci "liveComponent" viewMsg |> Async.AwaitTask

                  return state, ctx, ci
               }
              | Update msg ->
                async {
                  let! (state, cmd) = (updateH ctx ci msg state |> Async.AwaitTask)

                  let clientHub = ctx.RequestServices.GetService<ISocketHub> ()
                  let viewTemplate = viewH ctx ci state
                  let viewStr = Giraffe.GiraffeViewEngine.renderHtmlDocument viewTemplate
                  let viewMsg = {ComponentId = componentId; Data = viewStr}
                  do! clientHub.SendMessageToClient ci "liveComponent" viewMsg |> Async.AwaitTask

                  inbox.Post (Dispatch cmd)
                  return state, ctx, ci
                }
              | Dispatch (cmd: Cmd<'Msg>) ->
                async {
                  cmd |> List.iter (fun n -> n (Update >> inbox.Post) )
                  return state, ctx, ci
                }
            return! messageLoop (newState, ctx, ci) }

          let inState = Unchecked.defaultof<'State>
          let inCtx = Unchecked.defaultof<HttpContext>
          let inCi = Unchecked.defaultof<ClientInfo>
          messageLoop (inState, inCtx, inCi)
        )

        channel {
          join (fun ctx si -> task {
            let! res = joinH ctx si
            match res with
            | JoinResult.Ok ->
              stateMP.Post (Init (ctx, si))
              let! (s,cmd) = initH ctx si (Dispatch >> stateMP.Post)
              stateMP.Post (SetState s)
              stateMP.Post (Dispatch cmd)
            | _ ->
              ()
            return res
          })

          handle "liveComponent" (fun ctx si (msg: Message<LiveComponentMsg>) -> task {
            let m = mmH ctx si msg.Payload
            stateMP.Post (Update m)
            return ()
          })

          terminate (fun ctx si -> task {
            (stateMP :> System.IDisposable).Dispose()
            return ()
          })
        }

      { new ILiveComponenet with
          member __.InternalChannel with get () =  c
      }

  let liveComponent<'State, 'Msg> id = LiveComponenetBuilder<'State, 'Msg>(id)

