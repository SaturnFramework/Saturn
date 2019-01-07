namespace Saturn

open System
open SiteMap
open Microsoft.FSharp.Reflection
open System.Collections.Concurrent
open Microsoft.AspNetCore.Http

[<AutoOpen>]
module Controller =

  open Giraffe
  open FSharp.Control.Tasks.V2.ContextInsensitive
  open System.Threading.Tasks

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

  let except (actions: Action list) =
    let inputSet = Set actions
    if inputSet |> Set.contains All then []
    else
      let allSet = Set [Index;Show;Add;Edit;Create;Update;Patch;Delete;DeleteAll]
      allSet - inputSet |> Set.toList

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
    member __.CaseInsensitive (state) =
      {state with CaseInsensitive = true}

    ///Inject a controller into the routing table rooted at a given path. All of that controller's actions will be anchored off of the path as a prefix.
    [<CustomOperation("subController")>]
    member __.SubController(state, path, handler) =
      {state with SubControllers = (path, handler)::state.SubControllers}

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
        [Index; Show; Add; Edit; Create; Update; Delete;DeleteAll] |> List.fold (fun acc e -> addPlug acc e handler) state
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

    member private __.AddHandlerWithRoute<'Output> state action (handler: HttpContext -> Task<'Output>) route =
      let handler =
        match typeof<'Output> with
        | k when k = typeof<HttpContext option> -> fun _ ctx -> handler ctx |> unbox<HttpFuncResult>
        | _ -> fun _ ctx -> handler ctx |> response<'Output> ctx

      match state.Plugs.TryFind action with
      | Some acts ->
        // Apply route test before applying plugs
        let plugs = succeed |> List.foldBack (fun e acc -> acc >=> e) acts
        route >=> plugs >=> handler
      | None -> route >=> handler

    member private __.AddKeyHandler<'Output> state action (handler: HttpContext -> 'Key -> Task<'Output>) path =
      let route =
        if state.CaseInsensitive then
          routeCif (PrintfFormat<_,_,_,_,'Key> path)
        else
          routef (PrintfFormat<_,_,_,_,'Key> path)

      let handler =
        match typeof<'Output> with
        | k when k = typeof<HttpContext option> -> fun input _ ctx -> handler ctx (unbox<'Key> input) |> unbox<HttpFuncResult>
        | _ -> fun input _ ctx -> handler ctx (unbox<'Key> input) |> response<'Output> ctx

      match state.Plugs.TryFind action with
      | Some acts ->
        // Apply route test before applying plugs
        let plugs = succeed |> List.foldBack (fun e acc -> acc >=> e) acts
        route (fun key -> plugs >=> (handler key))

      | None -> route handler

    member this.Run (state: ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput>) : HttpHandler =
      let siteMap = HandlerMap()
      let addToSiteMap v p = siteMap.AddPath p v
      let keyFormat =
        match state with
        | { Show = None; Edit = None; Update = None; Delete = None; SubControllers = [] } -> None
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
            let route = if state.CaseInsensitive then routeCi "/" else route "/"
            if ctx.Request.Path.Value.EndsWith("/") then
              route next ctx
            else if (SubRouting.getNextPartOfPath ctx = "") then
              // TODO this could go away pending discussion about ctx.Request.Path modification.
              // Only change Path at the end of the road, otherwise we cannot have all plugs fire after route check.
              ctx.Request.Path <- PathString(ctx.Request.Path.Value + "/")
              route next ctx
            else
              route next ctx
        choose [
          yield GET >=> choose [
            let addToSiteMap = addToSiteMap "GET"

            if state.Add.IsSome then
              let path = "/add"
              addToSiteMap path
              yield this.AddHandlerWithRoute state Add state.Add.Value (route path)

            if state.Index.IsSome then
              addToSiteMap "/"
              yield this.AddHandlerWithRoute state Index state.Index.Value trailingSlashHandler

            if keyFormat.IsSome then
              if state.Edit.IsSome then
                let path = keyFormat.Value + "/edit"
                addToSiteMap path
                yield this.AddKeyHandler state Edit state.Edit.Value path
              if state.Show.IsSome then
                let path = keyFormat.Value
                addToSiteMap path
                yield this.AddKeyHandler state Show state.Show.Value path
          ]
          yield POST >=> choose [
            let addToSiteMap = addToSiteMap "POST"

            if state.Create.IsSome then
              addToSiteMap "/"
              yield this.AddHandlerWithRoute state Create state.Create.Value trailingSlashHandler

            if keyFormat.IsSome then
              if state.Update.IsSome then
                let path = keyFormat.Value
                addToSiteMap path
                yield this.AddKeyHandler state Update state.Update.Value path
          ]
          yield PATCH >=> choose [
            let addToSiteMap = addToSiteMap "PATCH"

            if keyFormat.IsSome then
              if state.Patch.IsSome then
                let path = keyFormat.Value
                addToSiteMap path
                yield this.AddKeyHandler state Patch state.Patch.Value path
          ]
          yield PUT >=> choose [
            let addToSiteMap = addToSiteMap "PUT"

            if keyFormat.IsSome then
              if state.Update.IsSome then
                let path = keyFormat.Value
                addToSiteMap path
                yield this.AddKeyHandler state Update state.Update.Value path
          ]
          yield DELETE >=> choose [
            let addToSiteMap = addToSiteMap "DELETE"

            if state.DeleteAll.IsSome then
              addToSiteMap "/"
              yield this.AddHandlerWithRoute state DeleteAll state.DeleteAll.Value trailingSlashHandler

            if keyFormat.IsSome then
              if state.Delete.IsSome then
                let path = keyFormat.Value
                addToSiteMap path
                yield this.AddKeyHandler state Delete state.Delete.Value path
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
            for (subPath, sCs) in state.SubControllers do
              if not (subPath.StartsWith("/")) then
                failwith (sprintf "Subcontroller route '%s' is not valid, these routes should start with a '/'." subPath)

              let fullPath = keyFormat.Value + subPath

              siteMap.Forward fullPath "" (sCs (Unchecked.defaultof<'Key>))
              yield
                if state.CaseInsensitive then
                  subRoutefCi (PrintfFormat<'Key -> obj,_,_,_,'Key> fullPath) (unbox<'Key> >> sCs)
                else
                  subRoutef (PrintfFormat<'Key -> obj,_,_,_,'Key> fullPath) (unbox<'Key> >> sCs)

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

  let controller<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> = ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput> ()
