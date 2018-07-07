namespace Saturn

open System
open SiteMap

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
    | Delete
    | DeleteAll
    | All

  type ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'DeleteOutput, 'DeleteAllOutput> = {
    Index: (HttpContext -> Task<'IndexOutput>) option
    Show: (HttpContext -> 'Key -> Task<'ShowOutput>) option
    Add: (HttpContext -> Task<'AddOutput>) option
    Edit: (HttpContext -> 'Key -> Task<'EditOutput>) option
    Create: (HttpContext -> Task<'CreateOutput>) option
    Update: (HttpContext -> 'Key -> Task<'UpdateOutput>) option
    Delete: (HttpContext -> 'Key -> Task<'DeleteOutput>) option
    DeleteAll: (HttpContext -> Task<'DeleteAllOutput>) option

    NotFoundHandler: HttpHandler option
    ErrorHandler: HttpContext -> Exception -> HttpFuncResult
    SubControllers : (string * ('Key -> HttpHandler)) list
    Plugs : Map<Action, HttpHandler list>
    Version: int option
  }

  type internal KeyType =
    | Bool
    | Char
    | String
    | Int32
    | Int64
    | Float
    | Guid

  let inline response<'a> ctx (input : System.Threading.Tasks.Task<'a>) =
      task {
        let! i = input
        return! Controller.response ctx i
      }

  type ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'DeleteOutput, 'DeleteAllOutput> internal () =




    member __.Yield(_) : ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'DeleteOutput, 'DeleteAllOutput> =
      { Index = None; Show = None; Add = None; Edit = None; Create = None; Update = None; Delete = None; DeleteAll = None; NotFoundHandler = None; Version = None; SubControllers = []; Plugs = Map.empty<_,_>; ErrorHandler = (fun _ ex -> raise ex) }



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
    member __.NotFoundHandler(state : ControllerState<_,_,_,_,_,_,_,_,_>, handler) =
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

    member __.Run (state: ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'DeleteOutput, 'DeleteAllOutput>) : HttpHandler =
      let siteMap = HandlerMap()
      let typ =
        match state with
        | { Show = None; Edit = None; Update = None; Delete = None; SubControllers = [] } -> None
        | _ ->
          match typeof<'Key> with
          | k when k = typeof<bool> -> Bool
          | k when k = typeof<char> -> Char
          | k when k = typeof<string> -> String
          | k when k = typeof<int32> -> Int32
          | k when k = typeof<int64> -> Int64
          | k when k = typeof<float> -> Float
          | k when k = typeof<System.Guid> -> Guid
          | k -> failwithf
                  "Type %A is not a supported type for controller<'T>. Supported types include bool, char, float, guid int32, int64, and string" k
          |> Some

      let addPlugs action handler =
        match state.Plugs.TryFind action with
        | Some acts -> (succeed |> List.foldBack (fun e acc -> acc >=> e) acts) >=> handler
        | None -> handler

      let initialController =
        choose [
          yield GET >=> choose [
            if state.Add.IsSome then
              siteMap.AddPath "/add" "GET"
              match typeof<'AddOutput> with
              | k when k = typeof<HttpContext option> -> yield addPlugs Add (route "/add" >=> (fun _ ctx -> state.Add.Value(ctx) |> unbox<HttpFuncResult> ))
              | _ -> yield addPlugs Add (route "/add" >=> (fun _ ctx -> state.Add.Value(ctx) |> response<_> ctx))

            if state.Edit.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool ->
                  siteMap.AddPath "/%b/edit" "GET"
                  match typeof<'EditOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Edit (routef "/%b/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Edit (routef "/%b/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Char ->
                  siteMap.AddPath "/%c/edit" "GET"
                  match typeof<'EditOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Edit (routef "/%c/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Edit (routef "/%c/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | String ->
                  siteMap.AddPath "/%s/edit" "GET"
                  match typeof<'EditOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Edit (routef "/%s/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Edit (routef "/%s/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int32 ->
                  siteMap.AddPath "/%i/edit" "GET"
                  match typeof<'EditOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Edit (routef "/%i/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Edit (routef "/%i/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int64 ->
                  siteMap.AddPath "/%d/edit" "GET"
                  match typeof<'EditOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Edit (routef "/%d/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Edit (routef "/%d/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Float ->
                  siteMap.AddPath "/%f/edit" "GET"
                  match typeof<'EditOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Edit (routef "/%f/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Edit (routef "/%f/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Guid ->
                  siteMap.AddPath "/%O/edit" "GET"
                  match typeof<'EditOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Edit (routef "/%O/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Edit (routef "/%O/edit" (fun input _ ctx -> state.Edit.Value ctx (unbox<'Key> input) |> response<_> ctx))
            if state.Show.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool ->
                  siteMap.AddPath "/%b" "GET"
                  match typeof<'ShowOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Show (routef "/%b" (fun input _ ctx ->  state.Show.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Show (routef "/%b" (fun input _ ctx -> state.Show.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Char ->
                  siteMap.AddPath "/%c" "GET"
                  match typeof<'ShowOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Show (routef "/%c" (fun input _ ctx ->  state.Show.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Show (routef "/%c" (fun input _ ctx -> state.Show.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | String ->
                  siteMap.AddPath "/%s" "GET"
                  match typeof<'ShowOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Show (routef "/%s" (fun input _ ctx ->  state.Show.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Show (routef "/%s" (fun input _ ctx -> state.Show.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int32 ->
                  siteMap.AddPath "/%i" "GET"
                  match typeof<'ShowOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Show (routef "/%i" (fun input _ ctx ->  state.Show.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Show (routef "/%i" (fun input _ ctx -> state.Show.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int64 ->
                  siteMap.AddPath "/%d" "GET"
                  match typeof<'ShowOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Show (routef "/%d" (fun input _ ctx ->  state.Show.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Show (routef "/%d" (fun input _ ctx -> state.Show.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Float ->
                  siteMap.AddPath "/%f" "GET"
                  match typeof<'ShowOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Show (routef "/%f" (fun input _ ctx ->  state.Show.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Show (routef "/%f" (fun input _ ctx -> state.Show.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Guid ->
                  siteMap.AddPath "/%O" "GET"
                  match typeof<'ShowOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Show (routef "/%O" (fun input _ ctx ->  state.Show.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Show (routef "/%O" (fun input _ ctx -> state.Show.Value ctx (unbox<'Key> input) |> response<_> ctx))
            if state.Index.IsSome then
              siteMap.AddPath "/" "GET"
              match typeof<'IndexOutput> with
              | k when k = typeof<HttpContext option> ->
                yield addPlugs Index (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Index.Value(ctx) |> unbox<HttpFuncResult>))
                yield addPlugs Index (route "/" >=> (fun _ ctx -> state.Index.Value(ctx) |> unbox<HttpFuncResult>))
              | _ ->
                yield addPlugs Index (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Index.Value(ctx) |> response<_> ctx))
                yield addPlugs Index (route "/" >=> (fun _ ctx -> state.Index.Value(ctx) |> response<_> ctx))
          ]
          yield POST >=> choose [
            if state.Create.IsSome then
              siteMap.AddPath "/" "POST"
              match typeof<'CreateOutput> with
              | k when k = typeof<HttpContext option> ->
                yield addPlugs Create (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Create.Value(ctx) |> unbox<HttpFuncResult>))
                yield addPlugs Create (route "/" >=> (fun _ ctx -> state.Create.Value(ctx) |> unbox<HttpFuncResult>))
              | _ ->
                yield addPlugs Create (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Create.Value(ctx) |> response<_> ctx))
                yield addPlugs Create (route "/" >=> (fun _ ctx -> state.Create.Value(ctx) |> response<_> ctx))

            if state.Update.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool ->
                  siteMap.AddPath "/%b" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Char ->
                  siteMap.AddPath "/%c" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | String ->
                  siteMap.AddPath "/%s" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int32 ->
                  siteMap.AddPath "/%i" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int64 ->
                  siteMap.AddPath "/%d" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Float ->
                  siteMap.AddPath "/%f" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Guid ->
                  siteMap.AddPath "/%O" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
          ]
          yield PATCH >=> choose [
            if state.Update.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool ->
                  siteMap.AddPath "/%b" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Char ->
                  siteMap.AddPath "/%c" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | String ->
                  siteMap.AddPath "/%s" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int32 ->
                  siteMap.AddPath "/%i" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int64 ->
                  siteMap.AddPath "/%d" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Float ->
                  siteMap.AddPath "/%f" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Guid ->
                  siteMap.AddPath "/%O" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
          ]
          yield PUT >=> choose [
            if state.Update.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool ->
                  siteMap.AddPath "/%b" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Char ->
                  siteMap.AddPath "/%c" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | String ->
                  siteMap.AddPath "/%s" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int32 ->
                  siteMap.AddPath "/%i" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int64 ->
                  siteMap.AddPath "/%d" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Float ->
                  siteMap.AddPath "/%f" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Guid ->
                  siteMap.AddPath "/%O" "POST"
                  match typeof<'UpdateOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ ->  yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value ctx (unbox<'Key> input) |> response<_> ctx))
          ]
          yield DELETE >=> choose [
            if state.DeleteAll.IsSome then
              siteMap.AddPath "/" "DELETE"
              match typeof<'DeleteAllOutput> with
              | k when k = typeof<HttpContext option> ->
                yield addPlugs DeleteAll (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.DeleteAll.Value(ctx) |> unbox<HttpFuncResult>))
                yield addPlugs DeleteAll (route "/" >=> (fun _ ctx -> state.DeleteAll.Value(ctx) |> unbox<HttpFuncResult>))
              | _ ->
                yield addPlugs DeleteAll (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.DeleteAll.Value(ctx) |> response<_> ctx))
                yield addPlugs DeleteAll (route "/" >=> (fun _ ctx -> state.DeleteAll.Value(ctx) |> response<_> ctx))
            if state.Delete.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool ->
                  siteMap.AddPath "/%b" "DELETE"
                  match typeof<'DeleteOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Delete (routef "/%b" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Delete (routef "/%b" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Char ->
                  siteMap.AddPath "/%c" "DELETE"
                  match typeof<'DeleteOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Delete (routef "/%c" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Delete (routef "/%c" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | String ->
                  siteMap.AddPath "/%s" "DELETE"
                  match typeof<'DeleteOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Delete (routef "/%s" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Delete (routef "/%s" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int32 ->
                  siteMap.AddPath "/%i" "DELETE"
                  match typeof<'DeleteOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Delete (routef "/%i" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Delete (routef "/%i" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Int64 ->
                  siteMap.AddPath "/%d" "DELETE"
                  match typeof<'DeleteOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Delete (routef "/%d" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Delete (routef "/%d" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Float ->
                  siteMap.AddPath "/%f" "DELETE"
                  match typeof<'DeleteOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Delete (routef "/%f" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Delete (routef "/%f" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> response<_> ctx))
                | Guid ->
                  siteMap.AddPath "/%O" "DELETE"
                  match typeof<'DeleteOutput> with
                  | k when k = typeof<HttpContext option> -> yield addPlugs Delete (routef "/%O" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> unbox<HttpFuncResult>))
                  | _ -> yield addPlugs Delete (routef "/%O" (fun input _ ctx -> state.Delete.Value ctx (unbox<'Key> input) |> response<_> ctx))
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
          for (sPath, sCs) in state.SubControllers do
            match typ with
            | None -> ()
            | Some k ->
              match k with
              | Bool ->
                let dummy = sCs (unbox<'Key> false)
                siteMap.Forward ("/%b" + sPath) "" dummy
                yield routef (PrintfFormat<bool -> obj, obj, obj, obj, bool>("/%b" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<bool -> string -> obj, obj, obj, obj, bool * string>("/%b" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Char ->
                let dummy = sCs (unbox<'Key> ' ')
                siteMap.Forward ("/%c" + sPath) "" dummy
                yield routef (PrintfFormat<char -> obj, obj, obj, obj, char>("/%c" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<char -> string -> obj, obj, obj, obj, char * string>("/%c" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | String ->
                let dummy = sCs (unbox<'Key> "")
                siteMap.Forward ("/%s" + sPath) "" dummy
                yield routef (PrintfFormat<string -> obj, obj, obj, obj, string>("/%s" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<string -> string -> obj, obj, obj, obj, string * string>("/%s" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Int32 ->
                let dummy = sCs (unbox<'Key> 0)
                siteMap.Forward ("/%i" + sPath) "" dummy
                yield routef (PrintfFormat<int -> obj, obj, obj, obj, int>("/%i" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<int -> string -> obj, obj, obj, obj, int * string>("/%i" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Int64 ->
                let dummy = sCs (unbox<'Key> 0L)
                siteMap.Forward ("/%d" + sPath) "" dummy
                yield routef (PrintfFormat<int64 -> obj, obj, obj, obj, int64>("/%d" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<int64 -> string -> obj, obj, obj, obj, int64 * string>("/%d" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Float ->
                let dummy = sCs (unbox<'Key> 0.)
                siteMap.Forward ("/%f" + sPath) "" dummy
                yield routef (PrintfFormat<float -> obj, obj, obj, obj, float>("/%f" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<float -> string -> obj, obj, obj, obj, float * string>("/%f" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Guid ->
                let dummy = sCs (unbox<'Key> Guid.Empty)
                siteMap.Forward ("/%O" + sPath) "" dummy
                yield routef (PrintfFormat<obj -> obj, obj, obj, obj, obj>("/%O" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<obj -> string -> obj, obj, obj, obj, obj * string>("/%O" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
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


  let controller<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'DeleteOutput, 'DeleteAllOutput> = ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'DeleteOutput, 'DeleteAllOutput> ()

