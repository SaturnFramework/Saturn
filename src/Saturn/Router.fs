namespace Saturn

open Giraffe.Core
open Giraffe.Routing
open System.Collections.Generic
open SiteMap
open System

[<AutoOpen>]
module Router =


  [<RequireQualifiedAccess>]
  type RouteType =
    | Get
    | Post
    | Put
    | Delete
    | Patch
    | Forward

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
          | RouteType.Post -> "POST"
          | RouteType.Put -> "PUT"
          | RouteType.Patch -> "PATCH"
          | RouteType.Delete -> "DELETE"
          | RouteType.Forward -> ""
        let routes, routesf = state.GetRoutes typ
        let routes = routes |> Seq.map (fun (p, lst) ->
          lst |> Seq.iter (fun l -> siteMap.Forward p v l)
          route p >=> (choose lst))
        let routesf = routesf |> Seq.map (fun (p, lst) ->
          lst |> Seq.iter (fun l ->
            try
              siteMap.Forward p v (tryDummy l)
            with
            | _ -> ())
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
      let pathces, patchesf = generateRoutes RouteType.Patch

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
          yield GET >=> choose [
            yield! gets
            yield! getsf
          ]
          yield POST >=> choose [
            yield! posts
            yield! postsf
          ]
          yield PATCH >=> choose [
            yield! pathces
            yield! patchesf
          ]
          yield PUT >=> choose [
            yield! puts
            yield! putsf
          ]
          yield DELETE >=> choose [
            yield! deletes
            yield! deletesf
          ]
          yield! forwards
          yield! forwardsf
          if state.NotFoundHandler.IsSome then
            siteMap.NotFound ()
            yield state.NotFoundHandler.Value
      ]
      let res = (fetchUrl |> List.foldBack (fun e acc -> acc >=> e) state.Pipelines) >=> lst
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

  [<ObsoleteAttribute("This construct is obsolete, use `router` instead")>]
  let scope = RouterBuilder()

  let router = RouterBuilder()
