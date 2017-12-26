namespace Saturn

module Router =

  open Giraffe.HttpHandlers
  open Giraffe.TokenRouter
  open System.Collections.Generic
  open Microsoft.AspNetCore.Http

  [<RequireQualifiedAccess>]
  type RouteType =
    | Get
    | Post
    | Put
    | Delete
    | Patch
    | Forward

  type ScopeState =
    { Routes: Dictionary<string * RouteType, HttpHandler list>
      RoutesF: Dictionary<string * RouteType, (obj -> HttpHandler) list>

      NotFoundHandler: HttpHandler
      Pipelines: HttpHandler list
    }
    with
      member internal state.GetRoutes(typ: RouteType) =
        let rts =
          state.Routes
          |> Seq.map(|KeyValue|)
          |> Seq.filter(fun ((_, t), _) -> t = typ )
          |> Seq.map (fun ((p, _), acts) -> (p, acts |> List.rev))
        let rtsf =
          state.RoutesF
          |> Seq.map(|KeyValue|)
          |> Seq.filter(fun ((_, t), _) -> t = typ )
          |> Seq.map (fun ((p, _), (acts)) -> (p, acts |> List.rev))
        rts,rtsf

  type ScopeBuilder internal () =

    let addRoute typ state path action : ScopeState =
      let action =  action |> List.foldBack (>=>) state.Pipelines
      let lst =
        match state.Routes.TryGetValue((path, typ)) with
        | false, _ -> []
        | true, lst -> lst
      state.Routes.[(path, typ)] <-  action::lst
      state

    let addRouteF typ state (path: PrintfFormat<_,_,_,_,'f>) action : ScopeState =
      let action = fun o -> (action o) |> List.foldBack (>=>) state.Pipelines
      let r = fun (o : obj) -> o |> unbox<'f> |> action
      let lst =
        match state.RoutesF.TryGetValue((path.Value, typ)) with
        | false, _ -> []
        | true, lst -> lst
      state.RoutesF.[(path.Value, typ)] <- r::lst
      state

    member __.Yield(_) : ScopeState =
      { Routes = Dictionary()
        RoutesF = Dictionary()
        Pipelines = []
        NotFoundHandler = setStatusCode 404 >=> text "Not found" }

    member __.Run(state : ScopeState) : HttpHandler =
      let generateRoutes typ =
        let routes, routesf = state.GetRoutes typ
        let routes = routes |> Seq.map (fun (p, lst) -> route p (choose lst))
        let routesf = routesf |> Seq.map (fun (p, lst) ->
          let pf = PrintfFormat<_,_,_,_,_> p
          let chooseF = fun o ->
            lst
            |> List.map (fun f -> f o)
            |> choose
          routefUnsafe pf chooseF
        )
        routes, routesf

      let gets, getsf = generateRoutes RouteType.Get
      let posts, postsf = generateRoutes RouteType.Post
      let puts, putsf = generateRoutes RouteType.Put
      let deletes, deletesf = generateRoutes RouteType.Put

      let forwards, _ = state.GetRoutes RouteType.Forward
      let forwards =
        forwards
        |> Seq.map (fun (p, lst) ->
          let act = ((fun nxt ctx ->
            let path = ctx.Request.Path.Value
            if path.StartsWith p then
              let newPath = path.Substring p.Length
              ctx.Request.Path <- PathString(newPath)
            nxt ctx)
            >=> choose lst )

          routef (PrintfFormat<_,_,_,_,string>(p + "%s")) (fun _ -> act ))

      let lst = [
        yield GET [
          yield! gets
          yield! getsf
          yield! forwards
        ]
        yield POST [
          yield! posts
          yield! postsf
          yield! forwards
        ]
        yield PUT [
          yield! puts
          yield! putsf
          yield! forwards
        ]
        yield DELETE [
          yield! deletes
          yield! deletesf
          yield! forwards
        ]
      ]
      Pipeline.fetchUrl >=> router state.NotFoundHandler lst

    ///Adds handler for `GET` request.
    [<CustomOperation("get")>]
    member __.Get(state, path : string, action: HttpHandler) : ScopeState =
      addRoute RouteType.Get state path action

    ///Adds handler for `GET` request.
    [<CustomOperation("getf")>]
    member __.GetF(state, path : PrintfFormat<_,_,_,_,'f>, action) : ScopeState =
      addRouteF RouteType.Get state path action

    ///Adds handler for `POST` request.
    [<CustomOperation("post")>]
    member __.Post(state, path : string, action: HttpHandler) : ScopeState =
      addRoute RouteType.Post state path action

    ///Adds handler for `POST` request.
    [<CustomOperation("postf")>]
    member __.PostF(state, path, action) : ScopeState =
      addRouteF RouteType.Post state path action

    ///Adds handler for `PUT` request.
    [<CustomOperation("put")>]
    member __.Put(state, path : string, action: HttpHandler) : ScopeState =
      addRoute RouteType.Put state path action

    ///Adds handler for `PUT` request.
    [<CustomOperation("putf")>]
    member __.PutF(state, path, action) : ScopeState =
      addRouteF RouteType.Put state path action

    ///Adds handler for `DELETE` request.
    [<CustomOperation("delete")>]
    member __.Delete(state, path : string, action: HttpHandler) : ScopeState =
      addRoute RouteType.Delete state path action

    ///Adds handler for `DELETE` request.
    [<CustomOperation("deletef")>]
    member __.DeleteF(state, path, action) : ScopeState =
      addRouteF RouteType.Delete state path action

    ///Adds handler for `PATCH` request.
    [<CustomOperation("patch")>]
    member __.Patch(state, path : string, action: HttpHandler) : ScopeState =
      addRoute RouteType.Patch state path action

    ///Adds handler for `PATCH` request.
    [<CustomOperation("patchf")>]
    member __.PatchF(state, path, action) : ScopeState =
      addRouteF RouteType.Patch state path action

    ///Forwards calls to different `scope`. Modifies the `HttpRequest.Path` to allow subrouting.
    [<CustomOperation("forward")>]
    member __.Forward(state, path : string, action : HttpHandler) : ScopeState =
      addRoute RouteType.Forward state path action

    ///Adds pipeline to the list of pipelines that will be used for every request
    [<CustomOperation("pipe_through")>]
    member __.PipeThrough(state, pipe) : ScopeState =
      {state with Pipelines = pipe::state.Pipelines}

    ///Adds error/not-found handler for current scope
    [<CustomOperation("error_handler")>]
    member __.ErrprHandler(state, handler) : ScopeState =
      {state with NotFoundHandler = handler}

  let scope = ScopeBuilder()
