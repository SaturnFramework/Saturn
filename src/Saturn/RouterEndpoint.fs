namespace Saturn.Endpoint

open Giraffe.Core
open Giraffe.EndpointRouting
open System.Collections.Generic
open System

[<AutoOpen>]
///Module containing `pipeline` computation expression
module Router =


  [<RequireQualifiedAccess>]
  ///Type representing route type, used in internal state of the `application` computation expression
  type RouteType =
    | Get
    | Post
    | Put
    | Delete
    | Patch

  ///Type representing internal state of the `router` computation expression
  type RouterState =
    { Routes: Dictionary<string * RouteType, HttpHandler list>
      RoutesF: Dictionary<string * RouteType, (obj -> HttpHandler) list>
      Forawrds: Dictionary<string, Endpoint list>

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

    let addForward state path action : RouterState =
      let lst =
        match state.Forawrds.TryGetValue((path)) with
        | false, _ -> []
        | true, lst -> lst
      state.Forawrds.[(path)] <-  action@lst
      state

    member __.Yield(_) : RouterState =
      { Routes = Dictionary()
        RoutesF = Dictionary()
        Forawrds = Dictionary()
        Pipelines = [] }

    member __.Run(state : RouterState) : Endpoint list =

      let generateRoutes typ =
        let addPipeline hndl = (Saturn.PipelineHelpers.fetchUrl |> List.foldBack (fun e acc -> acc >=> e) state.Pipelines) >=> hndl

        let v =
          match typ with
          | RouteType.Get -> "GET"
          | RouteType.Post -> "POST"
          | RouteType.Put -> "PUT"
          | RouteType.Patch -> "PATCH"
          | RouteType.Delete -> "DELETE"
        let routes, routesf = state.GetRoutes typ
        let routes = routes |> Seq.map (fun (p, lst) ->

          if lst.Length = 1 then
            route p (addPipeline lst.Head)
          else
            route p (addPipeline (choose lst)))
        let routesf = routesf |> Seq.map (fun (p, lst) ->
          let pf = PrintfFormat<_,_,_,_,_> p
          let chooseF = fun o ->
            lst
            |> List.map (fun f -> f o)
            |> choose
          routef pf chooseF
        )
        [ yield! routes; yield! routesf]

      let gets= generateRoutes RouteType.Get
      let posts = generateRoutes RouteType.Post
      let patches = generateRoutes RouteType.Patch

      let puts = generateRoutes RouteType.Put
      let deletes = generateRoutes RouteType.Delete

      let forwards =
        state.Forawrds
        |> Seq.map (fun (KeyValue (p, lst)) ->
          subRoute p lst
        )

      let lst =
        [
          for e in gets do
            yield GET => e
          for e in posts do
            yield POST => e
          for e in patches do
            yield PATCH => e
          for e in puts do
            yield PUT => e
          for e in deletes do
            yield DELETE => e

          yield! forwards
      ]

      lst

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

    ///Forwards calls to different `Endpoint`. Modifies the `HttpRequest.Path` to allow subrouting.
    [<CustomOperation("forward")>]
    member __.Forward(state, path : string, action : Endpoint) : RouterState =
      addForward state path [action]

    ///Forwards calls to different list of `Endpoint`. Modifies the `HttpRequest.Path` to allow subrouting.
    member __.Forward(state, path : string, actions : Endpoint list) : RouterState =
      addForward state path actions

    ///Adds pipeline to the list of pipelines that will be used for every request
    [<CustomOperation("pipe_through")>]
    member __.PipeThrough(state, pipe) : RouterState =
      {state with Pipelines = pipe::state.Pipelines}


  [<ObsoleteAttribute("This construct is obsolete, use `router` instead")>]
  let scope = RouterBuilder()

  ///Computation expression used to create routing in Saturn application
  let router = RouterBuilder()
