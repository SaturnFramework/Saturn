namespace Saturn.Endpoint

open System
open Microsoft.FSharp.Reflection
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Routing

[<AutoOpen>]
///Module with `controller` computation expression
module Controller =

  open Giraffe.Core
  open Giraffe.EndpointRouting
  open FSharp.Control.Tasks
  open System.Threading.Tasks

  ///Using `x-controller-version` for endpoint routing
  type ControllerVersionMetadata = {
    Value: string
  }

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
    ErrorHandler: (HttpContext -> Exception -> HttpFuncResult) option
    SubControllers : (string * ('Key -> HttpHandler)) list
    Plugs : Map<Action, HttpHandler list>
    Version: string option
  }

  let inline response<'a> ctx (input : Task<'a>) =
      task {
        let! i = input
        return! Saturn.ControllerHelpers.Controller.response ctx i
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
  /// let commentController userId = subcontroller {
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
      { Index = None; Show = None; Add = None; Edit = None; Create = None; Update = None; Patch = None; Delete = None; DeleteAll = None; NotFoundHandler = None; Version = None; SubControllers = []; Plugs = Map.empty<_,_>; ErrorHandler = None; }

    ///Operation that should render (or return in case of API controllers) list of data
    [<CustomOperation("index")>]
    member __.Index (state, (handler : HttpContext -> Task<'IndexOutput>)) =
      {state with Index = Some handler}

    ///Operation that should render (or return in case of API controllers) single entry of data
    [<CustomOperation("show")>]
    member __.Show (state, handler: HttpContext -> 'Key -> Task<'ShowOutput>) =
      {state with Show = Some handler}

    ///Operation that should render form for adding new item
    [<CustomOperation("add")>]
    member __.Add (state, handler: HttpContext -> Task<'AddOutput>) =
      {state with Add = Some handler}

    ///Operation that should render form for editing existing item
    [<CustomOperation("edit")>]
    member __.Edit (state, handler: HttpContext -> 'Key -> Task<'EditOutput>) =
      {state with Edit = Some handler}

    ///Operation that creates new item
    [<CustomOperation("create")>]
    member __.Create (state, handler: HttpContext -> Task<'CreateOutput>) =
      {state with Create = Some handler}

    ///Operation that updates existing item
    [<CustomOperation("update")>]
    member __.Update (state, handler: HttpContext -> 'Key -> Task<'UpdateOutput>) =
      {state with Update = Some handler}

    ///Operation that patches existing item
    [<CustomOperation("patch")>]
    member __.Patch (state, handler: HttpContext -> 'Key -> Task<'PatchOutput>) =
      {state with Patch = Some handler}

    ///Operation that deletes existing item
    [<CustomOperation("delete")>]
    member __.Delete (state, handler: HttpContext -> 'Key -> Task<'DeleteOutput>) =
      {state with Delete = Some handler}

    ///Operation that deletes all items
    [<CustomOperation("delete_all")>]
    member __.DeleteAll (state, handler: HttpContext -> Task<'DeleteAllOutput>) =
      {state with DeleteAll = Some handler}

    ///Define not-found handler for the controller
    [<CustomOperation("not_found_handler")>]
    member __.NotFoundHandler(state : ControllerState<_,_,_,_,_,_,_,_,_,_>, handler) =
      {state with NotFoundHandler = Some handler}

    ///Define error for the controller
    [<CustomOperation("error_handler")>]
    member __.ErrorHandler(state, handler: HttpContext -> Exception -> HttpFuncResult) =
      {state with ErrorHandler = Some handler}

    ///Define version of controller. Adds checking of `x-controller-version` header
    [<CustomOperation("version")>]
    member __.Version(state, version) =
      {state with Version = Some version}

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

    member private __.ActionToEndpoint action =
      match action with
      | Index -> (fun hdl -> GET  [route "/" hdl])
      | Add -> (fun hdl -> GET [route "/add" hdl])
      | Create -> (fun hdl -> POST [route "/" hdl])
      | DeleteAll -> (fun hdl -> DELETE [route "/" hdl])
      | Show | Edit |  Update | Patch | Delete -> failwith "Shouldn't happen - operations based on id"
      | All -> failwith "Shouldn't happen"

    member private __.ActionToIdEndpoint state action =
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
      let route = keyFormat.Value
      match action with
      | Show -> [fun hdl -> GET [routef (PrintfFormat<_,_,_,_,'Key> route) hdl]]
      | Edit -> [fun hdl -> GET [routef (PrintfFormat<_,_,_,_,'Key> (route + "/edit")) hdl]]
      | Update -> [fun hdl -> POST [routef (PrintfFormat<_,_,_,_,'Key> route) hdl];
                   fun hdl -> PUT [routef (PrintfFormat<_,_,_,_,'Key> route) hdl]]
      | Patch -> [fun hdl -> PATCH [routef (PrintfFormat<_,_,_,_,'Key> route) hdl]]
      | Delete -> [fun hdl -> DELETE [routef (PrintfFormat<_,_,_,_,'Key> route) hdl]]
      | Index | Add | Create | DeleteAll -> failwith "Shouldn't happen - operations based without id"
      | All -> failwith "Shouldn't happen"

    member private x.AddHandler<'Output> state action (actionHandler: HttpContext -> Task<'Output>) =

      //Get endpoint routing for the action
      let endpoint = x.ActionToEndpoint action

      //Plug automatic respond serialization
      let actionHandler =
        match typeof<'Output> with
        | k when k = typeof<HttpContext option> -> fun _ ctx -> actionHandler ctx |> unbox<HttpFuncResult>
        | _ -> fun _ ctx -> actionHandler ctx |> response<'Output> ctx

      //Add error handling
      let actionHandler nxt ctx =
        try
          actionHandler nxt ctx
        with
        | ex when state.ErrorHandler.IsSome -> state.ErrorHandler.Value ctx ex

      //Add plugs
      let actionHandler =
        match state.Plugs.TryFind action with
        | Some acts ->
          // Apply route test before applying plugs
          let plugs = Saturn.Common.succeed |> List.foldBack (fun e acc -> acc >=> e) acts
          plugs >=> actionHandler
        | None -> actionHandler

      let endp = endpoint actionHandler

      //Adds controler versioning
      let endp =
        match state.Version with
        | None -> endp
        | Some v ->
          endp |> addMetadata { Value = v.ToString()}
      endp

    member private x.AddKeyHandler<'Output> state action (actionHandler: HttpContext -> 'Key -> Task<'Output>) =
      let endpoint = x.ActionToIdEndpoint state action

      let actionHandler : 'Key -> HttpHandler =
        match typeof<'Output> with
        | k when k = typeof<HttpContext option> -> fun input _ ctx -> actionHandler ctx (unbox<'Key> input) |> unbox<HttpFuncResult>
        | _ -> fun input _ ctx -> actionHandler ctx (unbox<'Key> input) |> response<'Output> ctx

      //Add error handling
      let actionHandler id nxt ctx =
        try
          actionHandler id nxt ctx
        with
        | ex when state.ErrorHandler.IsSome -> state.ErrorHandler.Value ctx ex

      //Add plugs
      let actionHandler =
        match state.Plugs.TryFind action with
        | Some acts ->
          // Apply route test before applying plugs
          let plugs = Saturn.Common.succeed |> List.foldBack (fun e acc -> acc >=> e) acts
          fun key -> plugs >=> (actionHandler key)
        | None -> actionHandler

      endpoint |> List.map (fun e ->
        let endp = e actionHandler
        //Adds controler versioning
        let endp =
          match state.Version with
          | None -> endp
          | Some v ->
            endp |> addMetadata {Value = v.ToString()}
        endp)

    member this.Run (state: ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput>) : Endpoint list =
      let isKnownKey =
        match state with
        | { Show = None; Edit = None; Update = None; Delete = None; Patch = None; SubControllers = [] } -> false
        | _ ->
          match typeof<'Key> with
          | k when k = typeof<bool> -> true
          | k when k = typeof<char> -> true
          | k when k = typeof<string> -> true
          | k when k = typeof<int32> -> true
          | k when k = typeof<int64> -> true
          | k when k = typeof<float> -> true
          | k when k = typeof<Guid> -> true
          | k when k = typeof<uint64> -> true
          | k -> failwithf
                  "Type %A is not a supported type for controller<'T>. Supported types include bool, char, float, guid int32, int64, and string" k
      let keyFormat () =
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

      [
        //GET
        if state.Add.IsSome then
          yield this.AddHandler state Add state.Add.Value
        if state.Index.IsSome then
          yield this.AddHandler state Index state.Index.Value
        if isKnownKey then
          if state.Edit.IsSome then
            yield! this.AddKeyHandler state Edit state.Edit.Value
          if state.Show.IsSome then
            yield! this.AddKeyHandler state Show state.Show.Value

        //POST && PUT
        if state.Create.IsSome then
          yield this.AddHandler state Create state.Create.Value

        if isKnownKey then
          if state.Update.IsSome then
            yield! this.AddKeyHandler state Update state.Update.Value

        //PATCH
        if isKnownKey then
          if state.Patch.IsSome then
            yield! this.AddKeyHandler state Patch state.Patch.Value

        //DELETE
        if state.DeleteAll.IsSome then
          yield this.AddHandler state DeleteAll state.DeleteAll.Value

        if isKnownKey then
          if state.Delete.IsSome then
            yield! this.AddKeyHandler state Delete state.Delete.Value

        //????
        // if state.NotFoundHandler.IsSome then
        //   yield state.NotFoundHandler.Value

        for (subRoute, sCs) in state.SubControllers do
          if not (subRoute.StartsWith("/")) then
            failwith (sprintf "Subcontroller route '%s' is not valid, these routes should start with a '/'." subRoute)

          let fullRoute = keyFormat() + subRoute
          let anyRoute = keyFormat() + subRoute + "/{**any}"

          yield
            routef (PrintfFormat<'Key -> obj,_,_,_,'Key> anyRoute) (fun key -> fun nxt ctx -> task {
              let x = ctx.Request.Path.Value.LastIndexOf subRoute
              let v = ctx.Request.Path.Value.Substring(0,x + subRoute.Length)
              ctx.Items.Item "giraffe_route" <- v
              return! sCs key nxt ctx
            })
          yield
            routef (PrintfFormat<'Key -> obj,_,_,_,'Key> fullRoute) (fun key -> fun nxt ctx -> task {
              let v = ctx.Request.Path.Value
              let v =
                if (v.EndsWith "/") then
                  v.Substring(0, v.Length - 1)
                else
                  v
              ctx.Items.Item "giraffe_route" <- v
              return! sCs key nxt ctx
            })
    ]

  ///Computation expression used to create controllers
  let controller<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> = ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> ()

  ///Computation expression used to create HttpHandlers representing subcontrollers.
  let subcontroller<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> = Saturn.Controller.ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> ()


module ControllerDI =
  open System.Threading.Tasks
  open Giraffe

  type ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> with

    ///Operation that should render (or return in case of API controllers) list of data
    [<CustomOperation("index_di")>]
    member __.IndexDI (state, (handler : HttpContext -> 'Dependency -> Task<'IndexOutput>)) =
      {state with Index = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

    ///Operation that should render (or return in case of API controllers) single entry of data
    [<CustomOperation("show_di")>]
    member __.ShowDI (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'ShowOutput>) =
          {state with Show = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

    ///Operation that should render form for adding new item
    [<CustomOperation("add_di")>]
    member __.AddDI (state, handler : HttpContext -> 'Dependency -> Task<'AddOutput>) =
          {state with Add = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

    ///Operation that should render form for editing existing item
    [<CustomOperation("edit_di")>]
    member __.EditDI (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'EditOutput>) =
          {state with Edit = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

    ///Operation that creates new item
    [<CustomOperation("create_di")>]
    member __.CreateDI (state, handler: HttpContext -> 'Dependency -> Task<'AddOutput>) =
          {state with Create = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

    ///Operation that updates existing item
    [<CustomOperation("update_di")>]
    member __.UpdateDI (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'UpdateOutput>) =
          {state with Update = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

    ///Operation that patches existing item
    [<CustomOperation("patch_di")>]
    member __.PatchDI (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'PatchOutput>) =
          {state with Patch = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

    ///Operation that deletes existing item
    [<CustomOperation("delete_di")>]
    member __.DeleteDI (state, handler: HttpContext -> 'Dependency -> 'Key -> Task<'DeleteOutput>) =
          {state with Delete = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

    ///Operation that deletes all items
    [<CustomOperation("delete_all_di")>]
    member __.DeleteAllDI (state, handler: HttpContext -> 'Dependency -> Task<'DeleteAllOutput>) =
          {state with DeleteAll = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}

     ///Define not-found handler for the controller
    [<CustomOperation("not_found_handler_di")>]
    member __.NotFoundHandlerDI(state : ControllerState<_,_,_,_,_,_,_,_,_,_>, handler) =
      {state with NotFoundHandler = Some (Saturn.DependencyInjectionHelper.withInjectedDependencies handler)}

    ///Define error for the controller
    [<CustomOperation("error_handler_di")>]
    member __.ErrorHandlerDI(state, handler: HttpContext -> 'Dependency -> Exception -> HttpFuncResult) =
          {state with ErrorHandler = Some (Saturn.DependencyInjectionHelper.mapFromHttpContext handler)}
