namespace Saturn

open System
open SiteMap
open Microsoft.FSharp.Reflection
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http

[<AutoOpen>]
///Module with `controller` computation expression
module Controller =

  open Giraffe
  open FSharp.Control.Tasks.V2.ContextInsensitive
  open System.Threading.Tasks

  ///Type used for `plug` operation, allowing you to choose for which actions given plug should work
  type Action =
    | Index
    | Show
    | Add
    | Edit
    | Create
    | Update
    | Patch
    | Delete
    | DeleteAll
    | All

  ///Returns list of all actions except given actions.
  let except (actions: Action list) =
    let inputSet = Set actions
    if inputSet |> Set.contains All then []
    else
      let allSet = Set [Index;Show;Add;Edit;Create;Update;Patch;Delete;DeleteAll]
      allSet - inputSet |> Set.toList

  ///Type representing internal state of the `controller` computation expression
  type ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> = {
    Index: (HttpContext -> Task<'IndexOutput>) option
    Show: (HttpContext -> 'Key -> Task<'ShowOutput>) option
    Add: (HttpContext -> Task<'AddOutput>) option
    Edit: (HttpContext -> 'Key -> Task<'EditOutput>) option
    Create: (HttpContext -> Task<'CreateOutput>) option
    Update: (HttpContext -> 'Key -> Task<'UpdateOutput>) option
    Patch: (HttpContext -> 'Key -> Task<'PatchOutput>) option
    Delete: (HttpContext -> 'Key -> Task<'DeleteOutput>) option
    DeleteAll: (HttpContext -> Task<'DeleteAllOutput>) option

    NotFoundHandler: HttpHandler option
    ErrorHandler: HttpContext -> Exception -> HttpFuncResult
    SubControllers : (string * ('Key -> HttpHandler)) list
    Plugs : Map<Action, HttpHandler list>
    Version: string option
    CaseInsensitive: bool
  }

  let inline response<'a> ctx (input : Task<'a>) =
      task {
        let! i = input
        return! Controller.response ctx i
      }

  ///Computation expression used to create Saturn controllers - abstraction representing REST-ish endpoint for serving HTML views or returning data. It supports:
  ///
  /// * a set of predefined actions that are automatically mapped to the endpoints following standard conventions
  /// * embedding sub-controllers for modeling one-to-many relationships
  /// * versioning
  /// * adding plugs for a particular action which in principle provides the same mechanism as attributes in ASP.NET MVC applications
  /// * defining a common error handler for all actions
  /// * defining a not-found action
  ///
  ///The result of the computation expression is a standard Giraffe `HttpHandler`, which means that it's easily composable with other parts of the ecosytem.
  ///
  ///**Example:**
  ///
  /// ```fsharp
  /// let commentController userId = controller {
  ///     index (fun ctx -> (sprintf "Comment Index handler for user %i" userId ) |> Controller.text ctx)
  ///     add (fun ctx -> (sprintf "Comment Add handler for user %i" userId ) |> Controller.text ctx)
  ///     show (fun (ctx, id) -> (sprintf "Show comment %s handler for user %i" id userId ) |> Controller.text ctx)
  ///     edit (fun (ctx, id) -> (sprintf "Edit comment %s handler for user %i" id userId )  |> Controller.text ctx)
  /// }
  ///
  /// let userControllerVersion1 = controller {
  ///     version 1
  ///     subController "/comments" commentController
  ///
  ///     index (fun ctx -> "Index handler version 1" |> Controller.text ctx)
  ///     add (fun ctx -> "Add handler version 1" |> Controller.text ctx)
  ///     show (fun (ctx, id) -> (sprintf "Show handler version 1 - %i" id) |> Controller.text ctx)
  ///     edit (fun (ctx, id) -> (sprintf "Edit handler version 1 - %i" id) |> Controller.text ctx)
  /// }
  ///
  /// let userController = controller {
  ///     subController "/comments" commentController
  ///
  ///     plug [All] (setHttpHeader "user-controller-common" "123")
  ///     plug [Index; Show] (setHttpHeader "user-controller-specialized" "123")
  ///
  ///     index (fun ctx -> "Index handler no version" |> Controller.text ctx)
  ///     add (fun ctx -> "Add handler no version" |> Controller.text ctx)
  ///     show (fun (ctx, id) -> (sprintf "Show handler no version - %i" id) |> Controller.text ctx)
  ///     edit (fun (ctx, id) -> (sprintf "Edit handler no version - %i" id) |> Controller.text ctx)
  /// }
  /// ```
  type ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> internal () =

    member __.Yield(_) : ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> =
      { Index = None; Show = None; Add = None; Edit = None; Create = None; Update = None; Patch = None; Delete = None; DeleteAll = None; NotFoundHandler = None; Version = None; SubControllers = []; Plugs = Map.empty<_,_>; ErrorHandler = (fun _ ex -> raise ex); CaseInsensitive = false }

    ///Operation that should render (or return in case of API controllers) list of data
    [<CustomOperation("index")>]
    member __.Index (state, (handler : HttpContext -> Task<'IndexOutput>)) =
      {state with Index = Some handler}

    member x.Index (state, (handler : HttpContext -> 'Dependency -> Task<'IndexOutput>)) =
      {state with Index = Some (x.MapDependencyHandlerToHandler handler)}

    ///Operation that should render (or return in case of API controllers) single entry of data
    [<CustomOperation("show")>]
    member __.Show (state, handler: HttpContext -> 'Key -> Task<'ShowOutput>) =
      {state with Show = Some handler}

    member x.Show (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'ShowOutput>) =
      {state with Show = Some (x.MapDependencyHandlerToHandler' handler)}

    ///Operation that should render form for adding new item
    [<CustomOperation("add")>]
    member __.Add (state, handler: HttpContext -> Task<'AddOutput>) =
      {state with Add = Some handler}

    member x.Add (state, handler : HttpContext -> 'Dependency -> Task<'AddOutput>) =
      {state with Add = Some (x.MapDependencyHandlerToHandler handler)}

    ///Operation that should render form for editing existing item
    [<CustomOperation("edit")>]
    member __.Edit (state, handler: HttpContext -> 'Key -> Task<'EditOutput>) =
      {state with Edit = Some handler}

    member x.Edit (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'EditOutput>) =
      {state with Edit = Some (x.MapDependencyHandlerToHandler' handler)}

    ///Operation that creates new item
    [<CustomOperation("create")>]
    member __.Create (state, handler: HttpContext -> Task<'CreateOutput>) =
      {state with Create = Some handler}

    member x.Create (state, handler: HttpContext -> 'Dependency -> Task<'AddOutput>) =
      {state with Create = Some (x.MapDependencyHandlerToHandler handler)}

    ///Operation that updates existing item
    [<CustomOperation("update")>]
    member __.Update (state, handler: HttpContext -> 'Key -> Task<'UpdateOutput>) =
      {state with Update = Some handler}

    member x.Update (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'UpdateOutput>) =
      {state with Update = Some (x.MapDependencyHandlerToHandler' handler)}

    ///Operation that patches existing item
    [<CustomOperation("patch")>]
    member __.Patch (state, handler: HttpContext -> 'Key -> Task<'PatchOutput>) =
      {state with Patch = Some handler}

    member x.Patch (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'PatchOutput>) =
      {state with Patch = Some (x.MapDependencyHandlerToHandler' handler)}

    ///Operation that deletes existing item
    [<CustomOperation("delete")>]
    member __.Delete (state, handler: HttpContext -> 'Key -> Task<'DeleteOutput>) =
      {state with Delete = Some handler}

    member x.Delete (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'DeleteOutput>) =
      {state with Delete = Some (x.MapDependencyHandlerToHandler' handler)}

    ///Operation that deletes all items
    [<CustomOperation("delete_all")>]
    member __.DeleteAll (state, handler: HttpContext -> Task<'DeleteAllOutput>) =
      {state with DeleteAll = Some handler}

    member x.DeleteAll (state, handler: HttpContext -> 'Dependency -> Task<'DeleteAllOutput>) =
      {state with DeleteAll = Some (x.MapDependencyHandlerToHandler handler)}

    ///Define not-found handler for the controller
    [<CustomOperation("not_found_handler")>]
    member __.NotFoundHandler(state : ControllerState<_,_,_,_,_,_,_,_,_,_>, handler) =
      {state with NotFoundHandler = Some handler}

    ///Define error for the controller
    [<CustomOperation("error_handler")>]
    member __.ErrorHandler(state, handler: HttpContext -> Exception -> HttpFuncResult) =
      {state with ErrorHandler = handler}

    member x.ErrorHandler(state, handler: HttpContext -> 'Dependency -> Exception -> HttpFuncResult) =
      let h =
        fun ctx exc ->
          let d = x.GetDependency<'Dependency> ctx
          handler ctx d exc

      {state with ErrorHandler = h}

    ///Define version of controller. Adds checking of `x-controller-version` header
    [<CustomOperation("version")>]
    member __.Version(state, version) =
      {state with Version = Some version}

    ///Toggle case insensitve routing
    [<CustomOperation("case_insensitive")>]
    member __.CaseInsensitive (state : ControllerState<_,_,_,_,_,_,_,_,_,_> ) =
      {state with CaseInsensitive = true}

    ///Inject a controller into the routing table rooted at a given route. All of that controller's actions will be anchored off of the route as a prefix.
    [<CustomOperation("subController")>]
    member __.SubController(state, route, handler) =
      {state with SubControllers = (route, handler)::state.SubControllers}

    ///Add a plug that will be run on each of the provided actions.
    [<CustomOperation("plug")>]
    member __.Plug(state, actions, handler) =
      let addPlug state action handler =
        let newplugs =
          if state.Plugs.ContainsKey action then
            state.Plugs.Add(action, (handler::state.Plugs.[action]))
          else
            state.Plugs.Add(action,[handler])
        {state with Plugs = newplugs}

      if actions |> List.contains All then
        [Index;Show;Add;Edit;Create;Update;Patch;Delete;DeleteAll] |> List.fold (fun acc e -> addPlug acc e handler) state
      else
        actions |> List.fold (fun acc e -> addPlug acc e handler) state

    member private __.DependencyConstructors = ConcurrentDictionary<Type, HttpContext -> obj>()

    member private x.GetDependency<'Dependency>(ctx: HttpContext) =
      let getCtor (typ : Type) : (HttpContext -> obj) =
        if FSharpType.IsRecord typ then
          let fields = FSharpType.GetRecordFields typ
          fun (ctx: HttpContext) ->
            let flds = fields |> Array.map (fun t -> ctx.RequestServices.GetService(t.PropertyType))
            FSharpValue.MakeRecord(typ, flds)
        elif FSharpType.IsTuple typ then
          let fields = FSharpType.GetTupleElements typ
          fun (ctx: HttpContext) ->
            let flds = fields |> Array.map (ctx.RequestServices.GetService )
            FSharpValue.MakeTuple(flds, typ)
        elif typ = typeof<obj> then
          fun (_: HttpContext) ->
            Object()
        else
          fun (ctx: HttpContext) ->
            ctx.RequestServices.GetService typ

      let typ = typeof<'Dependency>
      if x.DependencyConstructors.ContainsKey typ then
        match x.DependencyConstructors.TryGetValue typ with
        | true, v -> v ctx |> unbox<'Dependency>
        | _ ->
          let c = getCtor typ
          let c = x.DependencyConstructors.AddOrUpdate(typ, c, Func<_,_,_>(fun _ _ -> c))
          c ctx |> unbox<'Dependency>
      else
        let c = getCtor typ
        let c = x.DependencyConstructors.AddOrUpdate(typ, c, Func<_,_,_>(fun _ _ -> c))
        c ctx |> unbox<'Dependency>

    member private x.MapDependencyHandlerToHandler<'Dependency, 'Output> (depHandler: HttpContext -> 'Dependency -> Task<'Output>) : HttpContext -> Task<'Output> =
      fun ctx ->
        let d = x.GetDependency<'Dependency> ctx
        depHandler ctx d

    member private x.MapDependencyHandlerToHandler'<'Dependency, 'Output> (depHandler: HttpContext -> 'Dependency -> 'Key -> Task<'Output>) : HttpContext -> 'Key -> Task<'Output> =
      fun ctx ->
        let d = x.GetDependency<'Dependency> ctx
        depHandler ctx d

    member inline private  __.RouteFunc state route =
      if state.CaseInsensitive then routeCi route else Giraffe.Routing.route route

    member inline private  __.FormattedRouteFunc state route routeHandler =
      if state.CaseInsensitive then routeCif route routeHandler else routef route routeHandler

    member private __.AddHandlerWithRouteHandler<'Output> state action (actionHandler: HttpContext -> Task<'Output>) routeHandler =
      let actionHandler =
        match typeof<'Output> with
        | k when k = typeof<HttpContext option> -> fun _ ctx -> actionHandler ctx |> unbox<HttpFuncResult>
        | _ -> fun _ ctx -> actionHandler ctx |> response<'Output> ctx

      match state.Plugs.TryFind action with
      | Some acts ->
        // Apply route test before applying plugs
        let plugs = succeed |> List.foldBack (fun e acc -> acc >=> e) acts
        routeHandler >=> plugs >=> actionHandler
      | None -> routeHandler >=> actionHandler

    member private x.AddKeyHandler<'Output> state action (actionHandler: HttpContext -> 'Key -> Task<'Output>) (route: string) =
      let actionHandler : 'Key -> HttpHandler =
        match typeof<'Output> with
        | k when k = typeof<HttpContext option> -> fun input _ ctx -> actionHandler ctx (unbox<'Key> input) |> unbox<HttpFuncResult>
        | _ -> fun input _ ctx -> actionHandler ctx (unbox<'Key> input) |> response<'Output> ctx

      let routeHandler (actionHandler: 'Key -> HttpHandler) =
        let routeHandler = x.FormattedRouteFunc state (PrintfFormat<_,_,_,_,'Key> route) actionHandler
        // All 'Key types except string don't match "/" so they always stay within a single path segment by design.
        if not (typeof<'Key> = typeof<string>)
        then routeHandler else
          let segmentRouteHandler =
            // Open issue in Giraffe for a routStartsWithf https://github.com/giraffe-fsharp/Giraffe/issues/341
            x.FormattedRouteFunc state (PrintfFormat<_,_,_,_,'Key * string> (route + "/%s")) (fst >> actionHandler)

          fun next ctx ->
            let hasTrailingSlash = (SubRouting.getNextPartOfPath ctx).LastIndexOf("/") = 0
            // If we still have more segments beyond our current segment we'll only match up to the next "/".
            // ASP.NET Core decodes everything but "/" characters for Request.Path, so we won't match those by accident here.
            (if hasTrailingSlash then routeHandler else segmentRouteHandler) next ctx

      match state.Plugs.TryFind action with
      | Some acts ->
        // Apply route test before applying plugs
        let plugs = succeed |> List.foldBack (fun e acc -> acc >=> e) acts
        routeHandler (fun key -> plugs >=> (actionHandler key))

      | None -> routeHandler actionHandler

    member this.Run (state: ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput>) : HttpHandler =
      let siteMap = HandlerMap()
      let addToSiteMap v p = siteMap.AddPath p v
      let keyFormat =
        match state with
        | { Show = None; Edit = None; Update = None; Delete = None; Patch = None; SubControllers = [] } -> None
        | _ ->
          match typeof<'Key> with
          | k when k = typeof<bool> -> "/%b"
          | k when k = typeof<char> -> "/%c"
          | k when k = typeof<string> -> "/%s"
          | k when k = typeof<int32> -> "/%i"
          | k when k = typeof<int64> -> "/%d"
          | k when k = typeof<float> -> "/%f"
          | k when k = typeof<Guid> -> "/%O"
          | k when k = typeof<uint64> -> "/%u"
          | k -> failwithf
                  "Type %A is not a supported type for controller<'T>. Supported types include bool, char, float, guid int32, int64, and string" k
          |> Some

      let initialController =
        let trailingSlashHandler : HttpHandler =
          fun next ctx ->
            let routeHandler = this.RouteFunc state "/"
            if ctx.Request.Path.Value.EndsWith("/") then
              routeHandler next ctx
            else if (SubRouting.getNextPartOfPath ctx = "") then
              // TODO this could go away pending discussion about ctx.Request.route modification.
              // Only change route at the end of the road, otherwise we cannot have all plugs fire after route check.
              ctx.Request.Path <- PathString(ctx.Request.Path.Value + "/")
              routeHandler next ctx
            else
              routeHandler next ctx
        choose [
          yield GET >=> choose [
            let addToSiteMap = addToSiteMap "GET"

            if state.Add.IsSome then
              let route = "/add"
              addToSiteMap route
              yield this.AddHandlerWithRouteHandler state Add state.Add.Value (this.RouteFunc state route)

            if state.Index.IsSome then
              addToSiteMap "/"
              yield this.AddHandlerWithRouteHandler state Index state.Index.Value trailingSlashHandler

            if keyFormat.IsSome then
              if state.Edit.IsSome then
                let route = keyFormat.Value + "/edit"
                addToSiteMap route
                yield this.AddKeyHandler state Edit state.Edit.Value route
              if state.Show.IsSome then
                let route = keyFormat.Value
                addToSiteMap route
                yield this.AddKeyHandler state Show state.Show.Value route
          ]
          yield POST >=> choose [
            let addToSiteMap = addToSiteMap "POST"

            if state.Create.IsSome then
              addToSiteMap "/"
              yield this.AddHandlerWithRouteHandler state Create state.Create.Value trailingSlashHandler

            if keyFormat.IsSome then
              if state.Update.IsSome then
                let route = keyFormat.Value
                addToSiteMap route
                yield this.AddKeyHandler state Update state.Update.Value route
          ]
          yield PATCH >=> choose [
            let addToSiteMap = addToSiteMap "PATCH"

            if keyFormat.IsSome then
              if state.Patch.IsSome then
                let route = keyFormat.Value
                addToSiteMap route
                yield this.AddKeyHandler state Patch state.Patch.Value route
          ]
          yield PUT >=> choose [
            let addToSiteMap = addToSiteMap "PUT"

            if keyFormat.IsSome then
              if state.Update.IsSome then
                let route = keyFormat.Value
                addToSiteMap route
                yield this.AddKeyHandler state Update state.Update.Value route
          ]
          yield DELETE >=> choose [
            let addToSiteMap = addToSiteMap "DELETE"

            if state.DeleteAll.IsSome then
              addToSiteMap "/"
              yield this.AddHandlerWithRouteHandler state DeleteAll state.DeleteAll.Value trailingSlashHandler

            if keyFormat.IsSome then
              if state.Delete.IsSome then
                let route = keyFormat.Value
                addToSiteMap route
                yield this.AddKeyHandler state Delete state.Delete.Value route
          ]
          if state.NotFoundHandler.IsSome then
            siteMap.NotFound ()
            yield state.NotFoundHandler.Value
      ]

      let controllerWithErrorHandler nxt ctx : HttpFuncResult =
        task {
          try
            return! initialController nxt ctx
          with
          | ex -> return! state.ErrorHandler ctx ex
        }

      let controllerWithSubs =
        choose [
          if keyFormat.IsSome then
            for (subRoute, sCs) in state.SubControllers do
              if not (subRoute.StartsWith("/")) then
                failwith (sprintf "Subcontroller route '%s' is not valid, these routes should start with a '/'." subRoute)

              let fullRoute = keyFormat.Value + subRoute

              siteMap.Forward fullRoute "" (sCs (Unchecked.defaultof<'Key>))
              yield
                if state.CaseInsensitive then
                  subRoutefCi (PrintfFormat<'Key -> obj,_,_,_,'Key> fullRoute) (unbox<'Key> >> sCs)
                else
                  subRoutef (PrintfFormat<'Key -> obj,_,_,_,'Key> fullRoute) (unbox<'Key> >> sCs)

          yield controllerWithErrorHandler
        ]

      let res =
        match state.Version with
        | None -> controllerWithSubs
        | Some v ->
          siteMap.Version <- Some v
          requireHeader "x-controller-version" (v.ToString()) >=> controllerWithSubs
      siteMap.SetKey res
      SiteMap.add siteMap
      res

  ///Computation expression used to create controllers
  let controller<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> = ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> ()
