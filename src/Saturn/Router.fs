namespace Saturn

open Giraffe.Core
open Giraffe.Routing
open System.Collections.Generic
open SiteMap
open System

[<AutoOpen>]
///Module containing `pipeline` computation expression
module Router =


  [<RequireQualifiedAccess>]
  ///Type representing route type, used in internal state of the `application` computation expression
  type RouteType =
    | Get
    | Head
    | GetOrHead
    | Post
    | Put
    | Delete
    | Patch
    | Forward

  ///Type representing internal state of the `router` computation expression
  type RouterState =
    { Routes: Dictionary<string * RouteType, HttpHandler list>
      RoutesF: Dictionary<string * RouteType, (obj -> HttpHandler) list>

      NotFoundHandler: HttpHandler option
      Pipelines: HttpHandler list
      CaseInsensitive: bool
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

  /// Computation expression used to create routing, combining `HttpHandlers`, `pipelines` and `controllers` together.
  ///
  /// The result of the computation expression is a standard Giraffe `HttpHandler`, which means that it's easily composable with other parts of the ecosytem.
  ///
  /// **Example:**
  ///
  /// ```fsharp
  /// let topRouter = router {
  ///     pipe_through headerPipe
  ///     not_found_handler (text "404")
  ///
  ///     get "/" helloWorld
  ///     get "/a" helloWorld2
  ///     getf "/name/%s" helloWorldName
  ///     getf "/name/%s/%i" helloWorldNameAge
  ///
  ///     //routers can be defined inline to simulate `subRoute` combinator
  ///     forward "/other" (router {
  ///         pipe_through otherHeaderPipe
  ///         not_found_handler (text "Other 404")
  ///
  ///         get "/" otherHelloWorld
  ///         get "/a" otherHelloWorld2
  ///     })
  ///
  ///     // or can be defined separatly and used as HttpHandler
  ///     forward "/api" apiRouter
  ///
  ///     // same with controllers
  ///     forward "/users" userController
  /// }
  /// ```
  type RouterBuilder internal () =

    let addRoute typ state path action : RouterState =
      let lst =
        match state.Routes.TryGetValue((path, typ)) with
        | false, _ -> []
        | true, lst -> lst
      state.Routes.[(path, typ)] <-  action::lst
      state

    let addRouteF typ state (path: PrintfFormat<_,_,_,_,'f>) action : RouterState =
      let r = fun (o : obj) -> o |> unbox<'f> |> action
      let lst =
        match state.RoutesF.TryGetValue((path.Value, typ)) with
        | false, _ -> []
        | true, lst -> lst
      state.RoutesF.[(path.Value, typ)] <- r::lst
      state

    member __.Yield(_) : RouterState =
      { Routes = Dictionary()
        RoutesF = Dictionary()
        Pipelines = []
        NotFoundHandler = None
        CaseInsensitive = false }

    member __.Run(state : RouterState) : HttpHandler =
      let siteMap = HandlerMap()

      let tryDummy (hndl : obj -> HttpHandler) =
        try
          hndl null
        with
        | _ ->
        try
          hndl (box 0)
        with
        | _ ->
        try
          hndl (box 0L)
        with
        | _ ->
        try
          hndl (box 0.)
        with
        | _ ->
        try
          hndl (box false)
        with
        | _ ->
        try
          hndl (box ' ')
        with
        | _ ->
        try
          hndl (box System.Guid.Empty)
        with
        | _ ->
          failwith "Couldn't evaluate handler"

      let route = if state.CaseInsensitive then routeCi else route
      let routefUnsafe = if state.CaseInsensitive then routefUnsafeCi else routefUnsafe
      let subRoute = if state.CaseInsensitive then subRouteCi else subRoute
      let subRoutefUnsafe = if state.CaseInsensitive then subRoutefUnsafeCi else subRoutefUnsafe

      let generateRoutes typ =
        let v =
          match typ with
          | RouteType.Get -> "GET"
          | RouteType.Head -> "HEAD"
          | RouteType.GetOrHead -> "GET_HEAD"
          | RouteType.Post -> "POST"
          | RouteType.Put -> "PUT"
          | RouteType.Patch -> "PATCH"
          | RouteType.Delete -> "DELETE"
          | RouteType.Forward -> ""
        let routes, routesf = state.GetRoutes typ
        let routes = routes |> Seq.map (fun (p, lst) ->

          lst |> Seq.iter (fun l -> siteMap.Forward p v l)
          if lst.Length = 1 then
            route p >=> lst.Head
          else
            route p >=> (choose lst))
        let routesf = routesf |> Seq.map (fun (p, lst) ->
          lst |> Seq.iter (fun l ->
            try
              siteMap.Forward p v (tryDummy l)
            with
            | _ -> ())
          let pf = PrintfFormat<_,_,_,_,_> p
          if lst.Length = 1 then
            routefUnsafe pf lst.Head
          else
            let chooseF = fun o ->
              lst
              |> List.map (fun f -> f o)
              |> choose
            routefUnsafe pf chooseF
        )
        routes, routesf

      let gets, getsf = generateRoutes RouteType.Get
      let heads, headsf = generateRoutes RouteType.Head
      let getOrHeads, getOrHeadsf = generateRoutes RouteType.GetOrHead
      let posts, postsf = generateRoutes RouteType.Post
      let patches, patchesf = generateRoutes RouteType.Patch

      let puts, putsf = generateRoutes RouteType.Put
      let deletes, deletesf = generateRoutes RouteType.Delete

      let forwards, forwardsf = state.GetRoutes RouteType.Forward
      let forwards =
        forwards
        |> Seq.map (fun (p, lst) ->
          lst |> Seq.iter (fun l -> siteMap.Forward p "" l)
          subRoute p (choose lst))

      let forwardsf =
        forwardsf |> Seq.map (fun (p, lst) ->
          lst |> Seq.iter (fun l ->
            try
              siteMap.Forward p "" ( tryDummy l)
            with
            | _ -> ())
          let pf = PrintfFormat<_,_,_,_,_> p
          let chooseF = fun o ->
            lst
            |> List.map (fun f -> f o)
            |> choose
          subRoutefUnsafe pf chooseF
        )

      let lst =
        choose [
          for e in gets do
            yield GET >=> e
          for e in getsf do
            yield GET >=> e

          for e in heads do
            yield HEAD >=> e
          for e in headsf do
            yield HEAD >=> e

          for e in getOrHeads do
            yield GET_HEAD >=> e
          for e in getOrHeadsf do
            yield GET_HEAD >=> e

          for e in posts do
            yield POST >=> e
          for e in postsf do
            yield POST >=> e

          for e in patches do
            yield PATCH >=> e
          for e in patchesf do
            yield PATCH >=> e

          for e in puts do
            yield PUT >=> e
          for e in putsf do
            yield PUT >=> e

          for e in deletes do
            yield DELETE >=> e
          for e in deletesf do
            yield DELETE >=> e

          yield! forwards
          yield! forwardsf
          if state.NotFoundHandler.IsSome then
            siteMap.NotFound ()
            yield state.NotFoundHandler.Value
      ]
      let res =
        if state.Pipelines.IsEmpty then
          lst
        else
          (succeed |> List.foldBack (fun e acc -> acc >=> e) state.Pipelines) >=> lst
      siteMap.SetKey res
      SiteMap.add siteMap
      res

    ///Adds handler for `GET` request.
    [<CustomOperation("get")>]
    member __.Get(state, path : string, action: HttpHandler) : RouterState =
      addRoute RouteType.Get state path action

    ///Adds handler for `GET` request.
    [<CustomOperation("getf")>]
    member __.GetF(state, path : PrintfFormat<_,_,_,_,'f>, action) : RouterState =
      addRouteF RouteType.Get state path action

    ///Adds handler for `HEAD` request.
    [<CustomOperation("head")>]
    member __.Head(state, path : string, action: HttpHandler) : RouterState =
      addRoute RouteType.Head state path action

    ///Adds handler for `HEAD` request.
    [<CustomOperation("headf")>]
    member __.HeadF(state, path : PrintfFormat<_,_,_,_,'f>, action) : RouterState =
      addRouteF RouteType.Head state path action

    ///Adds handler for either `GET` or `HEAD` request.
    [<CustomOperation("get_head")>]
    member __.GetOrHead(state, path : string, action: HttpHandler) : RouterState =
      addRoute RouteType.GetOrHead state path action

    ///Adds handler for either `GET` or `HEAD` request.
    [<CustomOperation("get_headf")>]
    member __.GetOrHeadF(state, path : PrintfFormat<_,_,_,_,'f>, action) : RouterState =
      addRouteF RouteType.GetOrHead state path action

    ///Adds handler for `POST` request.
    [<CustomOperation("post")>]
    member __.Post(state, path : string, action: HttpHandler) : RouterState =
      addRoute RouteType.Post state path action

    ///Adds handler for `POST` request.
    [<CustomOperation("postf")>]
    member __.PostF(state, path, action) : RouterState =
      addRouteF RouteType.Post state path action

    ///Adds handler for `PUT` request.
    [<CustomOperation("put")>]
    member __.Put(state, path : string, action: HttpHandler) : RouterState =
      addRoute RouteType.Put state path action

    ///Adds handler for `PUT` request.
    [<CustomOperation("putf")>]
    member __.PutF(state, path, action) : RouterState =
      addRouteF RouteType.Put state path action

    ///Adds handler for `DELETE` request.
    [<CustomOperation("delete")>]
    member __.Delete(state, path : string, action: HttpHandler) : RouterState =
      addRoute RouteType.Delete state path action

    ///Adds handler for `DELETE` request.
    [<CustomOperation("deletef")>]
    member __.DeleteF(state, path, action) : RouterState =
      addRouteF RouteType.Delete state path action

    ///Adds handler for `PATCH` request.
    [<CustomOperation("patch")>]
    member __.Patch(state, path : string, action: HttpHandler) : RouterState =
      addRoute RouteType.Patch state path action

    ///Adds handler for `PATCH` request.
    [<CustomOperation("patchf")>]
    member __.PatchF(state, path, action) : RouterState =
      addRouteF RouteType.Patch state path action

    ///Forwards calls to different `scope`. Modifies the `HttpRequest.Path` to allow subrouting.
    [<CustomOperation("forward")>]
    member __.Forward(state, path : string, action : HttpHandler) : RouterState =
      addRoute RouteType.Forward state path action

    ///Forwards calls to different `scope`. Modifies the `HttpRequest.Path` to allow subrouting.
    [<CustomOperation("forwardf")>]
    member __.Forwardf(state, path, action) : RouterState =
      addRouteF RouteType.Forward state path action

    ///Adds pipeline to the list of pipelines that will be used for every request
    [<CustomOperation("pipe_through")>]
    member __.PipeThrough(state, pipe) : RouterState =
      {state with Pipelines = pipe::state.Pipelines}

    ///Adds not-found handler for current scope
    [<CustomOperation("not_found_handler")>]
    member __.NotFoundHandler(state, handler) : RouterState =
      {state with NotFoundHandler = Some handler}

    ///Toggle case insensitve routing
    [<CustomOperation("case_insensitive")>]
    member __.CaseInsensitive (state) =
      {state with CaseInsensitive = true}

  ///Computation expression used to create routing in Saturn application
  let router = RouterBuilder()
