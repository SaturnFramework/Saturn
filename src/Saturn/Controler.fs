namespace Saturn

module Controler =

  open Microsoft.AspNetCore.Http
  open Giraffe
  open Giraffe.TokenRouter

  type ControlerState<'Key> = {
    Index: ((HttpFunc * HttpContext) -> HttpFuncResult) option
    Show: ((HttpFunc * HttpContext * 'Key) -> HttpFuncResult) option
    Add: ((HttpFunc * HttpContext) -> HttpFuncResult) option
    Edit: ((HttpFunc * HttpContext * 'Key) -> HttpFuncResult) option
    Create: ((HttpFunc * HttpContext) -> HttpFuncResult) option
    Update: ((HttpFunc * HttpContext * 'Key) -> HttpFuncResult) option
    Delete: ((HttpFunc * HttpContext * 'Key) -> HttpFuncResult) option
    NotFoundHandler: HttpHandler
  }

  type KeyType =
    | Bool
    | Char
    | String
    | Int32
    | Int64
    | Float
    | Guid

  type ControlerBuilder<'Key> internal () =
    member __.Yield(_) : ControlerState<'Key> =
      { Index = None; Show = None; Add = None; Edit = None; Create = None; Update = None; Delete = None; NotFoundHandler = setStatusCode 404 >=> text "Not found"  }

    member __.Run(state : ControlerState<'Key>) : HttpHandler =
      let typ =
        if typeof<'Key> = typeof<bool> then Bool
        elif typeof<'Key> = typeof<char> then Char
        elif typeof<'Key> = typeof<string> then String
        elif typeof<'Key> = typeof<int32> then Int32
        elif typeof<'Key> = typeof<int64> then Int64
        elif typeof<'Key> = typeof<float> then Float
        elif typeof<'Key> = typeof<System.Guid> then Guid
        else
          failwithf "Couldn't create router for controler. Key type not supported."

      let lst = [
        GET [
          if state.Index.IsSome then yield route "/" (fun nxt ctx -> state.Index.Value(nxt,ctx))
          if state.Add.IsSome then yield route "/add" (fun nxt ctx -> state.Add.Value(nxt,ctx))
          if state.Show.IsSome then
            match typ with
            | Bool -> yield routef "/%b" (fun input nxt ctx -> state.Show.Value(nxt,ctx, unbox<'Key> input) )
            | Char -> yield routef "/%c" (fun input nxt ctx -> state.Show.Value(nxt,ctx, unbox<'Key> input) )
            | String -> yield routef "/%s" (fun input nxt ctx -> state.Show.Value(nxt,ctx, unbox<'Key> input) )
            | Int32 -> yield routef "/%i" (fun input nxt ctx -> state.Show.Value(nxt,ctx, unbox<'Key> input) )
            | Int64 -> yield routef "/%d" (fun input nxt ctx -> state.Show.Value(nxt,ctx, unbox<'Key> input) )
            | Float -> yield routef "/%f" (fun input nxt ctx -> state.Show.Value(nxt,ctx, unbox<'Key> input) )
            | Guid -> yield routef "/%O" (fun input nxt ctx -> state.Show.Value(nxt,ctx, unbox<'Key> input) )
          if state.Edit.IsSome then
            match typ with
            | Bool -> yield routef "/%b/edit" (fun input nxt ctx -> state.Edit.Value(nxt,ctx, unbox<'Key> input) )
            | Char -> yield routef "/%c/edit" (fun input nxt ctx -> state.Edit.Value(nxt,ctx, unbox<'Key> input) )
            | String -> yield routef "/%s/edit" (fun input nxt ctx -> state.Edit.Value(nxt,ctx, unbox<'Key> input) )
            | Int32 -> yield routef "/%i/edit" (fun input nxt ctx -> state.Edit.Value(nxt,ctx, unbox<'Key> input) )
            | Int64 -> yield routef "/%d/edit" (fun input nxt ctx -> state.Edit.Value(nxt,ctx, unbox<'Key> input) )
            | Float -> yield routef "/%f/edit" (fun input nxt ctx -> state.Edit.Value(nxt,ctx, unbox<'Key> input) )
            | Guid -> yield routef "/%O/edit" (fun input nxt ctx -> state.Edit.Value(nxt,ctx, unbox<'Key> input) )
        ]
        POST [
          if state.Create.IsSome then yield route "/" (fun nxt ctx -> state.Create.Value(nxt,ctx))
        ]
        //TODO: Add Patch
        PUT [
          if state.Update.IsSome then
            match typ with
            | Bool -> yield routef "/%b" (fun input nxt ctx -> state.Update.Value(nxt,ctx, unbox<'Key> input) )
            | Char -> yield routef "/%c" (fun input nxt ctx -> state.Update.Value(nxt,ctx, unbox<'Key> input) )
            | String -> yield routef "/%s" (fun input nxt ctx -> state.Update.Value(nxt,ctx, unbox<'Key> input) )
            | Int32 -> yield routef "/%i" (fun input nxt ctx -> state.Update.Value(nxt,ctx, unbox<'Key> input) )
            | Int64 -> yield routef "/%d" (fun input nxt ctx -> state.Update.Value(nxt,ctx, unbox<'Key> input) )
            | Float -> yield routef "/%f" (fun input nxt ctx -> state.Update.Value(nxt,ctx, unbox<'Key> input) )
            | Guid -> yield routef "/%O" (fun input nxt ctx -> state.Update.Value(nxt,ctx, unbox<'Key> input) )
        ]
        DELETE [
          if state.Delete.IsSome then
            match typ with
            | Bool -> yield routef "/%b" (fun input nxt ctx -> state.Delete.Value(nxt,ctx, unbox<'Key> input) )
            | Char -> yield routef "/%c" (fun input nxt ctx -> state.Delete.Value(nxt,ctx, unbox<'Key> input) )
            | String -> yield routef "/%s" (fun input nxt ctx -> state.Delete.Value(nxt,ctx, unbox<'Key> input) )
            | Int32 -> yield routef "/%i" (fun input nxt ctx -> state.Delete.Value(nxt,ctx, unbox<'Key> input) )
            | Int64 -> yield routef "/%d" (fun input nxt ctx -> state.Delete.Value(nxt,ctx, unbox<'Key> input) )
            | Float -> yield routef "/%f" (fun input nxt ctx -> state.Delete.Value(nxt,ctx, unbox<'Key> input) )
            | Guid -> yield routef "/%O" (fun input nxt ctx -> state.Delete.Value(nxt,ctx, unbox<'Key> input) )
        ]
      ]
      router state.NotFoundHandler lst

    [<CustomOperation("index")>]
    member __.Index (state : ControlerState<'Key>, handler) =
      {state with Index = Some handler}

    [<CustomOperation("show")>]
    member __.Show (state : ControlerState<'Key>, handler) =
      {state with Show = Some handler}

    [<CustomOperation("add")>]
    member __.Add (state : ControlerState<'Key>, handler) =
      {state with Add = Some handler}

    [<CustomOperation("edit")>]
    member __.Edit (state : ControlerState<'Key>, handler) =
      {state with Edit = Some handler}

    [<CustomOperation("create")>]
    member __.Create (state : ControlerState<'Key>, handler) =
      {state with Create = Some handler}

    [<CustomOperation("update")>]
    member __.Update (state : ControlerState<'Key>, handler) =
      {state with Update = Some handler}

    [<CustomOperation("delete")>]
    member __.Delete (state : ControlerState<'Key>, handler) =
      {state with Delete = Some handler}

    [<CustomOperation("error_handler")>]
    member __.ErrprHandler(state : ControlerState<'Key>, handler) =
      {state with NotFoundHandler = handler}

  let controler<'Key> = ControlerBuilder<'Key> ()

