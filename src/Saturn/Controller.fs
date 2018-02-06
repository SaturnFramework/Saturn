namespace Saturn

module Controller =

  open Microsoft.AspNetCore.Http
  open Giraffe

  type ControllerState<'Key> = {
    Index: (HttpContext -> HttpFuncResult) option
    Show: (HttpContext * 'Key -> HttpFuncResult) option
    Add: (HttpContext -> HttpFuncResult) option
    Edit: (HttpContext * 'Key -> HttpFuncResult) option
    Create: (HttpContext -> HttpFuncResult) option
    Update: (HttpContext * 'Key -> HttpFuncResult) option
    Delete: (HttpContext * 'Key -> HttpFuncResult) option
    NotFoundHandler: HttpHandler option
    Version: int option
  }

  type KeyType =
    | Bool
    | Char
    | String
    | Int32
    | Int64
    | Float
    | Guid

  type ControllerBuilder<'Key> internal () =
    member __.Yield(_) : ControllerState<'Key> =
      { Index = None; Show = None; Add = None; Edit = None; Create = None; Update = None; Delete = None; NotFoundHandler = None; Version = None }

    member __.Run(state : ControllerState<'Key>) : HttpHandler =
      let typ =
        if typeof<'Key> = typeof<bool> then Bool
        elif typeof<'Key> = typeof<char> then Char
        elif typeof<'Key> = typeof<string> then String
        elif typeof<'Key> = typeof<int32> then Int32
        elif typeof<'Key> = typeof<int64> then Int64
        elif typeof<'Key> = typeof<float> then Float
        elif typeof<'Key> = typeof<System.Guid> then Guid
        else
          failwithf "Couldn't create router for controller. Key type not supported."

      let lst =
        choose [
          yield GET >=> choose [
            if state.Add.IsSome then yield route "/add" >=> (fun _ ctx -> state.Add.Value(ctx))
            if state.Edit.IsSome then
              match typ with
              | Bool -> yield routef "/%b/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input) )
              | Char -> yield routef "/%c/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input) )
              | String -> yield routef "/%s/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input) )
              | Int32 -> yield routef "/%i/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input) )
              | Int64 -> yield routef "/%d/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input) )
              | Float -> yield routef "/%f/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input) )
              | Guid -> yield routef "/%O/edit" (fun input _ ctx -> state.Edit.Value(ctx, unbox<'Key> input) )
            if state.Show.IsSome then
              match typ with
              | Bool -> yield routef "/%b" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input) )
              | Char -> yield routef "/%c" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input) )
              | String -> yield routef "/%s" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input) )
              | Int32 -> yield routef "/%i" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input) )
              | Int64 -> yield routef "/%d" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input) )
              | Float -> yield routef "/%f" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input) )
              | Guid -> yield routef "/%O" (fun input _ ctx -> state.Show.Value(ctx, unbox<'Key> input) )
            if state.Index.IsSome then
              yield route "" >=> (fun _ ctx -> ctx.Request.Path <- PathString(ctx.Request.Path.ToString() + "/"); state.Index.Value(ctx))
              yield route "/" >=> (fun _ ctx -> state.Index.Value(ctx))
          ]
          yield POST >=> choose [
            if state.Create.IsSome then yield route "/" >=> (fun _ ctx -> state.Create.Value(ctx))
            if state.Update.IsSome then
              match typ with
              | Bool -> yield routef "/%b" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Char -> yield routef "/%c" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | String -> yield routef "/%s" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Int32 -> yield routef "/%i" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Int64 -> yield routef "/%d" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Float -> yield routef "/%f" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Guid -> yield routef "/%O" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
          ]
          yield PATCH >=> choose [
            if state.Update.IsSome then
              match typ with
              | Bool -> yield routef "/%b" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Char -> yield routef "/%c" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | String -> yield routef "/%s" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Int32 -> yield routef "/%i" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Int64 -> yield routef "/%d" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Float -> yield routef "/%f" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Guid -> yield routef "/%O" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
          ]
          yield PUT >=> choose [
            if state.Update.IsSome then
              match typ with
              | Bool -> yield routef "/%b" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Char -> yield routef "/%c" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | String -> yield routef "/%s" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Int32 -> yield routef "/%i" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Int64 -> yield routef "/%d" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Float -> yield routef "/%f" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
              | Guid -> yield routef "/%O" (fun input _ ctx -> state.Update.Value(ctx, unbox<'Key> input) )
          ]
          yield DELETE >=> choose [
            if state.Delete.IsSome then
              match typ with
              | Bool -> yield routef "/%b" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input) )
              | Char -> yield routef "/%c" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input) )
              | String -> yield routef "/%s" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input) )
              | Int32 -> yield routef "/%i" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input) )
              | Int64 -> yield routef "/%d" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input) )
              | Float -> yield routef "/%f" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input) )
              | Guid -> yield routef "/%O" (fun input _ ctx -> state.Delete.Value(ctx, unbox<'Key> input) )
          ]
          if state.NotFoundHandler.IsSome then yield state.NotFoundHandler.Value
      ]
      match state.Version with
      | None -> lst
      | Some v ->
        Pipeline.requireHeader "x-controller-version" (v.ToString()) >=> lst

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

    ///Define error/not-found handler for the controller
    [<CustomOperation("error_handler")>]
    member __.ErrprHandler(state : ControllerState<'Key>, handler) =
      {state with NotFoundHandler = Some handler}

    ///Define version of controller. Adds checking of `x-controller-version` header
    [<CustomOperation("version")>]
    member __.Version(state, version) = 
      {state with Version = Some version}

  let controller<'Key> = ControllerBuilder<'Key> ()

