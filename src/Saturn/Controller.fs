namespace Saturn

open System
open SiteMap
open System.Collections.Concurrent
open Microsoft.FSharp.Reflection

[<AutoOpen>]
module Controller =

  open Microsoft.AspNetCore.Http
  open Giraffe
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

  type ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput, 'IndexDependency, 'ShowDependency, 'AddDependency, 'EditDependency, 'CreateDependency, 'UpdateDependency, 'PatchDependency, 'DeleteDependency, 'DeleteAllDependency> = {
    Index: (HttpContext -> 'IndexDependency -> Task<'IndexOutput>) option
    Show: (HttpContext -> 'Key -> 'ShowDependency -> Task<'ShowOutput>) option
    Add: (HttpContext -> 'AddDependency -> Task<'AddOutput>) option
    Edit: (HttpContext -> 'Key -> 'EditDependency -> Task<'EditOutput>) option
    Create: (HttpContext -> 'CreateDependency -> Task<'CreateOutput>) option
    Update: (HttpContext -> 'Key -> 'UpdateDependency -> Task<'UpdateOutput>) option
    Patch: (HttpContext -> 'Key -> 'PatchDependency -> Task<'PatchOutput>) option
    Delete: (HttpContext -> 'Key -> 'DeleteDependency -> Task<'DeleteOutput>) option
    DeleteAll: (HttpContext -> 'DeleteAllDependency -> Task<'DeleteAllOutput>) option

    NotFoundHandler: HttpHandler option
    ErrorHandler: HttpContext -> Exception -> HttpFuncResult
    SubControllers : (string * ('Key -> HttpHandler)) list
    Plugs : Map<Action, HttpHandler list>
    Version: string option
  }

  let inline response<'a> ctx (input : Task<'a>) =
      task {
        let! i = input
        return! Controller.response ctx i
      }

  type ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput, 'IndexDependency, 'ShowDependency, 'AddDependency, 'EditDependency, 'CreateDependency, 'UpdateDependency, 'PatchDependency, 'DeleteDependency, 'DeleteAllDependency> internal () =
    member __.Yield(_) : ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput, 'IndexDependency, 'ShowDependency, 'AddDependency, 'EditDependency, 'CreateDependency, 'UpdateDependency, 'PatchDependency, 'DeleteDependency, 'DeleteAllDependency> =
      { Index = None; Show = None; Add = None; Edit = None; Create = None; Update = None; Patch = None; Delete = None; DeleteAll = None; NotFoundHandler = None; Version = None; SubControllers = []; Plugs = Map.empty<_,_>; ErrorHandler = (fun _ ex -> raise ex) }

    ///Operation that should render (or return in case of API controllers) list of data
    [<CustomOperation("index")>]
    member __.Index (state, handler) =
      {state with Index = Some handler}

    ///Operation that should render (or return in case of API controllers) single entry of data
    [<CustomOperation("show")>]
    member __.Show (state, handler) =
      {state with Show = Some handler}

    ///Operation that should render form for adding new item
    [<CustomOperation("add")>]
    member __.Add (state, handler) =
      {state with Add = Some handler}

    ///Operation that should render form for editing existing item
    [<CustomOperation("edit")>]
    member __.Edit (state, handler) =
      {state with Edit = Some handler}

    ///Operation that creates new item
    [<CustomOperation("create")>]
    member __.Create (state, handler) =
      {state with Create = Some handler}

    ///Operation that updates existing item
    [<CustomOperation("update")>]
    member __.Update (state, handler) =
      {state with Update = Some handler}

    ///Operation that patches existing item
    [<CustomOperation("patch")>]
    member __.Patch (state, handler) =
      {state with Patch = Some handler}

    ///Operation that deletes existing item
    [<CustomOperation("delete")>]
    member __.Delete (state, handler) =
      {state with Delete = Some handler}

    ///Operation that deletes all items
    [<CustomOperation("delete_all")>]
    member __.DeleteAll (state, handler) =
      {state with DeleteAll = Some handler}

    ///Define not-found handler for the controller
    [<CustomOperation("not_found_handler")>]
    member __.NotFoundHandler(state : ControllerState<_,_,_,_,_,_,_,_,_,_,_,_,_,_,_,_,_,_,_>, handler) =
      {state with NotFoundHandler = Some handler}

    ///Define error for the controller
    [<CustomOperation("error_handler")>]
    member __.ErrorHandler(state, handler) =
      {state with ErrorHandler = handler}

    ///Define version of controller. Adds checking of `x-controller-version` header
    [<CustomOperation("version")>]
    member __.Version(state, version) =
      {state with Version = Some version}

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
        [Index; Show; Add; Edit; Create; Update; Delete] |> List.fold (fun acc e -> addPlug acc e handler) state
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
        else
          failwith "Dependency type must be record or type"
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

    member private x.AddHandler<'Output, 'Dependency> state action (handler: HttpContext -> 'Dependency -> Task<'Output>) path =
      let route = route path

      let handler =
        match typeof<'Output> with
        | k when k = typeof<HttpContext option> -> fun _ ctx ->
          let dep = x.GetDependency<'Dependency> ctx
          handler ctx dep |> unbox<HttpFuncResult>
        | _ -> fun _ ctx ->
          let dep = x.GetDependency ctx
          handler ctx dep |> response<'Output> ctx

      match state.Plugs.TryFind action with
      | Some acts -> (succeed |> List.foldBack (fun e acc -> acc >=> e) acts) >=> route >=> handler
      | None -> route >=> handler

    member private x.AddKeyHandler<'Output, 'Dependency> state action (handler: HttpContext -> 'Key -> 'Dependency -> Task<'Output>) path =
      let route = routef (PrintfFormat<_,_,_,_,'Key> path)

      let handler =
        match typeof<'Output> with
        | k when k = typeof<HttpContext option> -> fun input _ ctx ->
          let dep = x.GetDependency ctx
          handler ctx (unbox<'Key> input) dep |> unbox<HttpFuncResult>
        | _ -> fun input _ ctx ->
          let dep = x.GetDependency ctx
          handler ctx (unbox<'Key> input) dep |> response<'Output> ctx

      match state.Plugs.TryFind action with
      | Some acts -> (succeed |> List.foldBack (fun e acc -> acc >=> e) acts) >=> route handler
      | None -> route handler

    member this.Run (state: ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput,  'IndexDependency, 'ShowDependency, 'AddDependency, 'EditDependency, 'CreateDependency, 'UpdateDependency, 'PatchDependency, 'DeleteDependency, 'DeleteAllDependency>) : HttpHandler =
      let siteMap = HandlerMap()
      let addToSiteMap v p = siteMap.AddPath p v
      let keyFormat, stringConvert =
        match state with
        | { Show = None; Edit = None; Update = None; Delete = None; SubControllers = [] } -> None, None
        | _ ->
          // Parameterizing `string` here so we don't constrain 'Key to SRTP ^Key when we would have used it later on
          match typeof<'Key> with
          | k when k = typeof<bool> -> "/%b", string<bool> :> obj
          | k when k = typeof<char> -> "/%c", string<char> :> obj
          | k when k = typeof<string> -> "/%s", string<string> :> obj
          | k when k = typeof<int32> -> "/%i", string<int32> :> obj
          | k when k = typeof<int64> -> "/%d", string<int64> :> obj
          | k when k = typeof<float> -> "/%f", string<float> :> obj
          | k when k = typeof<Guid> -> "/%O", string<Guid> :> obj
          | k -> failwithf
                  "Type %A is not a supported type for controller<'T>. Supported types include bool, char, float, guid int32, int64, and string" k
          |> fun (keyFormat, stringConvert) -> (Some keyFormat, Some (unbox<'Key -> string> stringConvert))

      let initialController =
        choose [
          yield GET >=> choose [
            let addToSiteMap = addToSiteMap "GET"

            if state.Add.IsSome then
              let path = "/add"
              addToSiteMap path
              yield this.AddHandler state Add state.Add.Value path
            if state.Index.IsSome then
              let path = "/"
              let handler (ctx: HttpContext) = ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Index.Value(ctx)
              addToSiteMap path
              yield this.AddHandler state Index handler ""
              yield this.AddHandler state Index state.Index.Value path

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
              let path = "/"
              let handler (ctx: HttpContext) = ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Create.Value(ctx)
              addToSiteMap path
              yield this.AddHandler state Create handler ""
              yield this.AddHandler state Create state.Create.Value path

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
              let path = "/"
              let handler (ctx: HttpContext) = ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.DeleteAll.Value(ctx)
              addToSiteMap path
              yield this.AddHandler state DeleteAll handler ""
              yield this.AddHandler state DeleteAll state.DeleteAll.Value path

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
            let stringConvert = stringConvert.Value
            for (sPath, sCs) in state.SubControllers do
              if not (sPath.StartsWith("/")) then
                failwith (sprintf "Subcontroller route '%s' is not valid, these routes should start with a '/'." sPath)

              let path = keyFormat.Value
              let dummy = sCs (unbox<'Key> Unchecked.defaultof<'Key>)
              siteMap.Forward (path + sPath) "" dummy

              yield routef (PrintfFormat<'Key -> obj,_,_,_,'Key> (path + sPath))
                      (fun input -> subRoute ("/" + (stringConvert input) + sPath) (sCs (unbox<'Key> input)))

              yield routef (PrintfFormat<'Key -> string -> obj,_,_,_,'Key * string> (path + sPath + "%s"))
                      (fun (input, _) -> subRoute ("/" + (stringConvert input) + sPath) (sCs (unbox<'Key> input)))

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


  let controller<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput, 'IndexDependency, 'ShowDependency, 'AddDependency, 'EditDependency, 'CreateDependency, 'UpdateDependency, 'PatchDependency, 'DeleteDependency, 'DeleteAllDependency> = ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput, 'IndexDependency, 'ShowDependency, 'AddDependency, 'EditDependency, 'CreateDependency, 'UpdateDependency, 'PatchDependency, 'DeleteDependency, 'DeleteAllDependency> ()

