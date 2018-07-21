namespace Saturn

open Saturn
open Giraffe
open System
open Microsoft.AspNetCore.Http
open System.Threading.Tasks
open Microsoft.Azure.WebJobs.Host

module AzureFunctions =

  type FunctionState = {
    Logger: TraceWriter option
    Router: HttpHandler option
    ErrorHandler: (System.Exception -> HttpHandler)
    NotFoundHandler: HttpHandler
    HostPrefix: string
  }

  type FunctionBuilder internal () =
    member val LogWriter : TraceWriter option = None with get,set

    member __.Yield(_) =
        let errorHandler (ex : Exception) =
            __.LogWriter |> Option.iter (fun logger -> logger.Error("An unhandled exception has occurred while executing the request.", ex))
            clearResponse >=> Giraffe.HttpStatusCodeHandlers.ServerErrors.INTERNAL_ERROR ex.Message

        let notFoundHandler =
            clearResponse >=> Giraffe.HttpStatusCodeHandlers.RequestErrors.NOT_FOUND "Not found"
        {Logger = None; Router = None; ErrorHandler = errorHandler; NotFoundHandler = notFoundHandler; HostPrefix = "/api"}

    member __.Run(state: FunctionState) : (HttpRequest -> Task<HttpResponse>) =
      let next : HttpContext -> Task<HttpContext option> = Some >> Task.FromResult
      fun req ->
        let r =
          match state.Router with
          | Some r -> r
          | None -> failwith "Router needs to be defined"
        let r = router {
          pipe_through (fun nxt ctx -> state.Logger |> Option.iter (fun log -> ctx.Items.["TraceWriter"] <- log); nxt ctx)
          forward state.HostPrefix r
        }
        task {
          try
            let! result = r next req.HttpContext
            match result with
            | None ->
              let! errorResult = state.NotFoundHandler next req.HttpContext
              match errorResult with
              | None -> return failwith "Internal error"
              | Some ctx -> return ctx.Response
            | Some ctx -> return ctx.Response
          with
          | ex ->
            let! errorResult = state.ErrorHandler ex next req.HttpContext
            match errorResult with
            | None -> return failwith "Internal error"
            | Some ctx -> return ctx.Response
        }
    ///Defines top-level router used for the function
    [<CustomOperation("use_router")>]
    member __.Router(state : FunctionState, handler) =
      {state with Router = Some handler}

    ///Adds error handler for the function
    [<CustomOperation("error_handler")>]
    member __.ErrorHandler(state : FunctionState, handler) =
      {state with ErrorHandler = handler}

    ///Adds not found handler for the function
    [<CustomOperation("not_found_handler")>]
    member __.NotFoundHandler(state : FunctionState, handler) =
      {state with NotFoundHandler = handler}

    ///Adds logger for the function. Used for error reporting and passed to the actions as `ctx.Items.["TraceWriter"]`
    [<CustomOperation("logger")>]
    member __.Logger(state : FunctionState, logger) =
      __.LogWriter <- Some logger
      {state with Logger = Some logger}

    ///Adds prefix for the endpoint. By default Azure Functions are using `/api` prefix.
    [<CustomOperation("host_prefix")>]
    member __.HostPrefix(state : FunctionState, prefix) =
      {state with HostPrefix = prefix}

  let azureFunction = FunctionBuilder()