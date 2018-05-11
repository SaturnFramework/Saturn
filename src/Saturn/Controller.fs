namespace Saturn

open System

[<AutoOpen>]
module Controller =

  open Microsoft.AspNetCore.Http
  open Giraffe

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

  type ControllerState<'Key> = {
    Index: (HttpContext -> HttpFuncResult) option
    Show: (HttpContext * 'Key -> HttpFuncResult) option
    Add: (HttpContext -> HttpFuncResult) option
    Edit: (HttpContext * 'Key -> HttpFuncResult) option
    Create: (HttpContext -> HttpFuncResult) option
    Update: (HttpContext * 'Key -> HttpFuncResult) option
    Delete: (HttpContext * 'Key -> HttpFuncResult) option
    DeleteAll: (HttpContext -> HttpFuncResult) option
    NotFoundHandler: HttpHandler option
    ErrorHandler: (HttpContext * Exception) -> HttpFuncResult
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

  type ControllerBuilder<'Key> internal () =
    member __.Yield(_) : ControllerState<'Key> =
      { Index = None; Show = None; Add = None; Edit = None; Create = None; Update = None; Delete = None; DeleteAll = None; NotFoundHandler = None; Version = None; SubControllers = []; Plugs = Map.empty<_,_>; ErrorHandler = fun (_, ex) -> raise ex }

    member __.Run(state : ControllerState<'Key>) : HttpHandler =
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
        | Some acts -> (succeed |> List.foldBack (>=>) acts ) >=> handler
        | None -> handler

      let initialController =
        choose [
          yield GET >=> choose [
            if state.Add.IsSome then yield addPlugs Add (route "/add" >=> (fun _ ctx -> state.Add.Value(ctx)))
            if state.Edit.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool -> yield addPlugs Edit (routef "/%b/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input)))
                | Char -> yield addPlugs Edit (routef "/%c/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input)))
                | String -> yield addPlugs Edit (routef "/%s/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input)))
                | Int32 -> yield addPlugs Edit (routef "/%i/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input)))
                | Int64 -> yield addPlugs Edit (routef "/%d/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input)))
                | Float -> yield addPlugs Edit (routef "/%f/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input)))
                | Guid -> yield addPlugs Edit (routef "/%O/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input)))
            if state.Show.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool -> yield addPlugs Show (routef "/%b" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input)))
                | Char -> yield addPlugs Show (routef "/%c" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input)))
                | String -> yield addPlugs Show (routef "/%s" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input)))
                | Int32 -> yield addPlugs Show (routef "/%i" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input)))
                | Int64 -> yield addPlugs Show (routef "/%d" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input)))
                | Float -> yield addPlugs Show (routef "/%f" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input)))
                | Guid -> yield addPlugs Show (routef "/%O" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input)))
            if state.Index.IsSome then
              yield addPlugs Index (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Index.Value(ctx)))
              yield addPlugs Index (route "/" >=> (fun _ ctx -> state.Index.Value(ctx)))
          ]
          yield POST >=> choose [
            if state.Create.IsSome then
              yield addPlugs Create (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Create.Value(ctx)))
              yield addPlugs Create (route "/" >=> (fun _ ctx -> state.Create.Value(ctx)))

            if state.Update.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool -> yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Char -> yield addPlugs Update (routef "/%c" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | String -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Int32 -> yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Int64 -> yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Float -> yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Guid -> yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
          ]
          yield PATCH >=> choose [
            if state.Update.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool -> yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Char -> yield addPlugs Update (routef "/%c" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | String -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Int32 -> yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Int64 -> yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Float -> yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Guid -> yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
          ]
          yield PUT >=> choose [
            if state.Update.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool -> yield addPlugs Update (routef "/%b" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Char -> yield addPlugs Update (routef "/%c" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | String -> yield addPlugs Update (routef "/%s" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Int32 -> yield addPlugs Update (routef "/%i" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Int64 -> yield addPlugs Update (routef "/%d" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Float -> yield addPlugs Update (routef "/%f" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
                | Guid -> yield addPlugs Update (routef "/%O" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input)))
          ]
          yield DELETE >=> choose [
            if state.DeleteAll.IsSome then
              yield addPlugs DeleteAll (route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.DeleteAll.Value(ctx)))
              yield addPlugs DeleteAll (route "/" >=> (fun _ ctx -> state.DeleteAll.Value(ctx)))
            if state.Delete.IsSome then
              match typ with
              | None -> ()
              | Some k ->
                match k with
                | Bool -> yield addPlugs Delete (routef "/%b" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input)))
                | Char -> yield addPlugs Delete (routef "/%c" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input)))
                | String -> yield addPlugs Delete (routef "/%s" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input)))
                | Int32 -> yield addPlugs Delete (routef "/%i" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input)))
                | Int64 -> yield addPlugs Delete (routef "/%d" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input)))
                | Float -> yield addPlugs Delete (routef "/%f" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input)))
                | Guid -> yield addPlugs Delete (routef "/%O" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input)))
          ]
          if state.NotFoundHandler.IsSome then yield state.NotFoundHandler.Value
      ]

      let controllerWithErrorHandler nxt ctx : HttpFuncResult =
        task {
          try
            return! initialController nxt ctx
          with
          | ex -> return! state.ErrorHandler(ctx, ex)
        }

      let controllerWithSubs =
        choose [
          for (sPath, sCs) in state.SubControllers do
            match typ with
            | None -> ()
            | Some k ->
              match k with
              | Bool ->
                yield routef (PrintfFormat<bool -> obj, obj, obj, obj, bool>("/%b" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<bool -> string -> obj, obj, obj, obj, bool * string>("/%b" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Char ->
                yield routef (PrintfFormat<char -> obj, obj, obj, obj, char>("/%c" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<char -> string -> obj, obj, obj, obj, char * string>("/%c" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | String ->
                yield routef (PrintfFormat<string -> obj, obj, obj, obj, string>("/%s" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<string -> string -> obj, obj, obj, obj, string * string>("/%s" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Int32 ->
                yield routef (PrintfFormat<int -> obj, obj, obj, obj, int>("/%i" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<int -> string -> obj, obj, obj, obj, int * string>("/%i" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Int64 ->
                yield routef (PrintfFormat<int64 -> obj, obj, obj, obj, int64>("/%d" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<int64 -> string -> obj, obj, obj, obj, int64 * string>("/%d" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Float ->
                yield routef (PrintfFormat<float -> obj, obj, obj, obj, float>("/%f" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<float -> string -> obj, obj, obj, obj, float * string>("/%f" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
              | Guid ->
                yield routef (PrintfFormat<obj -> obj, obj, obj, obj, obj>("/%O" + sPath)) (fun input -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
                yield routef (PrintfFormat<obj -> string -> obj, obj, obj, obj, obj * string>("/%O" + sPath + "%s")) (fun (input, _) -> subRoute ("/" + (string input) + sPath) (sCs (unbox<'Key> input)))
          yield controllerWithErrorHandler
        ]

      match state.Version with
      | None -> controllerWithSubs
      | Some v ->
        requireHeader "x-controller-version" (v.ToString()) >=> controllerWithSubs

    ///Operation that should render (or return in case of API controllers) list of data
    [<CustomOperation("index")>]
    member __.Index (state : ControllerState<'Key>, handler) =
      {state with Index = Some handler}

    ///Operation that should render (or return in case of API controllers) single entry of data
    [<CustomOperation("show")>]
    member __.Show (state : ControllerState<'Key>, handler) =
      {state with Show = Some handler}

    ///Operation that should render form for adding new item
    [<CustomOperation("add")>]
    member __.Add (state : ControllerState<'Key>, handler) =
      {state with Add = Some handler}

    ///Operation that should render form for editing existing item
    [<CustomOperation("edit")>]
    member __.Edit (state : ControllerState<'Key>, handler) =
      {state with Edit = Some handler}

    ///Operation that creates new item
    [<CustomOperation("create")>]
    member __.Create (state : ControllerState<'Key>, handler) =
      {state with Create = Some handler}

    ///Operation that updates existing item
    [<CustomOperation("update")>]
    member __.Update (state : ControllerState<'Key>, handler) =
      {state with Update = Some handler}

    ///Operation that deletes existing item
    [<CustomOperation("delete")>]
    member __.Delete (state : ControllerState<'Key>, handler) =
      {state with Delete = Some handler}

    ///Operation that deletes all items
    [<CustomOperation("delete_all")>]
    member __.DeleteAll (state : ControllerState<'Key>, handler) =
      {state with DeleteAll = Some handler}

    ///Define not-found handler for the controller
    [<CustomOperation("not_found_handler")>]
    member __.NotFoundHandler(state : ControllerState<'Key>, handler) =
      {state with NotFoundHandler = Some handler}

    ///Define error for the controller
    [<CustomOperation("error_handler")>]
    member __.ErrorHandler(state : ControllerState<'Key>, handler) =
      {state with ErrorHandler = handler}

    ///Define version of controller. Adds checking of `x-controller-version` header
    [<CustomOperation("version")>]
    member __.Version(state, version) =
      {state with Version = Some version}

    [<CustomOperation("subController")>]
    member __.SubController(state, path, handler) =
      {state with SubControllers = (path, handler)::state.SubControllers}

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


  let controller<'Key> = ControllerBuilder<'Key> ()

