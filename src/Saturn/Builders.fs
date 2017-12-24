module Saturn.Router

open Giraffe.HttpHandlers
open Giraffe.TokenRouter
open Giraffe.Tasks
open System.Collections.Generic

type PipelineBuilder () =
  member __.Yield(_) : HttpHandler =
    fun nxt cntx -> task {return Some cntx}

  [<CustomOperation("plug")>]
  member __.Plug(state, plug) : HttpHandler  = state >=> plug

  [<CustomOperation("text")>]  member __.Text(state, cnt) : HttpHandler  = state >=> (text cnt)


let pipeline = PipelineBuilder()

[<RequireQualifiedAccess>]
type RouteType =
  | Get
  | Post
  | Put
  | Delete
  | Patch
  | NoMethod

type ScopeState = {
  Routes: Dictionary<string * RouteType, HttpHandler list>
  RoutesF: Dictionary<string * RouteType, (obj -> HttpHandler) list>

  NotFoundHandler: HttpHandler
  Pipelines: HttpHandler list
}

type ScopeBuilder(scope) =

  member __.Yield(_) : ScopeState =
    { Routes = Dictionary()
      RoutesF = Dictionary()
      Pipelines = []
      NotFoundHandler = setStatusCode 404 >=> text "Not found" }

  member __.Run(state : ScopeState) : HttpHandler =
    let lst = []
      // yield GET (List.rev state.Get)
      // yield POST (List.rev state.Post)
      // yield PUT (List.rev state.Put)
      // yield DELETE (List.rev state.Delete)
      // yield! List.rev state.NoMethod
    // ]
    Giraffe.HttpHandlers.route scope >=> (router state.NotFoundHandler lst)

  [<CustomOperation("get")>]
  member __.Get(state, path : string, action: HttpHandler) : ScopeState =
    let action =  action |> List.foldBack (>=>) state.Pipelines
    let lst =
      match state.Routes.TryGetValue((path, RouteType.Get)) with
      | false, _ -> []
      | true, lst -> lst
    state.Routes.Add((path, RouteType.Get), action::lst )
    state

  [<CustomOperation("getf")>]
  member __.GetF(state, path : PrintfFormat<_,_,_,_,'f>, action) : ScopeState =
    let action = fun o -> (action o) |> List.foldBack (>=>) state.Pipelines
    let r = fun (o : obj) -> o |> unbox<'f> |> action
    let lst =
      match state.RoutesF.TryGetValue((path.Value, RouteType.Get)) with
      | false, _ -> []
      | true, lst -> lst
    state.RoutesF.Add((path.Value, RouteType.Get), r::lst )
    state

  // [<CustomOperation("post")>]
  // member __.Post(state, path : string, action: HttpHandler) : RouterState =
  //   let action =  action |> List.foldBack (>=>) state.Pipelines
  //   let r = route path action
  //   {state with Post = r::state.Post}

  // [<CustomOperation("postf")>]
  // member __.PostF(state, path, action) : RouterState =
  //   let action = fun o -> (action o) |> List.foldBack (>=>) state.Pipelines
  //   let r = routef path action
  //   {state with Post = r::state.Post}

  // [<CustomOperation("put")>]
  // member __.Put(state, path : string, action: HttpHandler) : RouterState =
  //   let action =  action |> List.foldBack (>=>) state.Pipelines
  //   let r = route path action
  //   {state with Put = r::state.Put}

  // [<CustomOperation("putf")>]
  // member __.Put(state, path, action) : RouterState =
  //   let action = fun o -> (action o) |> List.foldBack (>=>) state.Pipelines
  //   let r = routef path action
  //   {state with Put = r::state.Put}

  // [<CustomOperation("delete")>]
  // member __.Delete(state, path : string, action: HttpHandler) : RouterState =
  //   let action =  action |> List.foldBack (>=>) state.Pipelines
  //   let r = route path action
  //   {state with Delete = r::state.Delete}

  // [<CustomOperation("deletef")>]
  // member __.Delete(state, path, action) : RouterState =
  //   let action = fun o -> (action o) |> List.foldBack (>=>) state.Pipelines
  //   let r = routef path action
  //   {state with Delete = r::state.Delete}

  // [<CustomOperation("patch")>]
  // member __.Patch(state, path : string, action: HttpHandler) : RouterState =
  //   let action =  action |> List.foldBack (>=>) state.Pipelines
  //   let r = route path action
  //   {state with Patch = r::state.Patch}

  // [<CustomOperation("patchf")>]
  // member __.Patch(state, path, action) : RouterState =
  //   let action = fun o -> (action o) |> List.foldBack (>=>) state.Pipelines
  //   let r = routef path action
  //   {state with Patch = r::state.Patch}

  // [<CustomOperation("route")>]
  // member __.Route(state, path, action) : RouterState =
  //   let action =  action |> List.foldBack (>=>) state.Pipelines
  //   let r = route path action
  //   {state with NoMethod = r::state.NoMethod}

  // [<CustomOperation("routef")>]
  // member __.Route(state, path, action) : RouterState =
  //   let action = fun o -> (action o) |> List.foldBack (>=>) state.Pipelines
  //   let r = routef path action
  //   {state with NoMethod = r::state.NoMethod}

  // [<CustomOperation("plug")>]
  // member __.Plug(state, action) : RouterState =
  //   let action =  action |> List.foldBack (>=>) state.Pipelines
  //   let r = route "/" action
  //   {state with NoMethod = r::state.NoMethod}

  [<CustomOperation("pipe_through")>]
  member __.PipeThrough(state, pipe) : ScopeState =
    {state with Pipelines = pipe::state.Pipelines}


let scope path = ScopeBuilder(path)
