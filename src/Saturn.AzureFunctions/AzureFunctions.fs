namespace Saturn

open Saturn
open Giraffe
open FSharp.Control.Tasks
open System
open Microsoft.AspNetCore.Http
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Mvc

module AzureFunctions =

  type FunctionState = {
    Logger: ILogger option
    Router: HttpHandler option
    ErrorHandler: (System.Exception -> HttpHandler)
    NotFoundHandler: HttpHandler
    HostPrefix: string
    JsonSerializer: Json.ISerializer
    XmlSerializer: Xml.ISerializer
    NegotiationConfig: INegotiationConfig
  }

  type FunctionBuilder internal () =
    let mvcNoopResult =
      { new IActionResult with
          member __.ExecuteResultAsync(_) = Task.CompletedTask
      }

    member val LogWriter : ILogger option = None with get,set

    member __.Yield(_) =
        let errorHandler (ex : Exception) =
            __.LogWriter |> Option.iter (fun logger -> logger.LogError("An unhandled exception has occurred while executing the request.", ex))
            clearResponse >=> Giraffe.HttpStatusCodeHandlers.ServerErrors.INTERNAL_ERROR ex.Message

        let notFoundHandler =
            clearResponse >=> Giraffe.HttpStatusCodeHandlers.RequestErrors.NOT_FOUND "Not found"
        {Logger = None; Router = None; ErrorHandler = errorHandler; NotFoundHandler = notFoundHandler; HostPrefix = "/api"; JsonSerializer = NewtonsoftJson.Serializer(NewtonsoftJson.Serializer.DefaultSettings); XmlSerializer = SystemXml.Serializer(SystemXml.Serializer.DefaultSettings);  NegotiationConfig = DefaultNegotiationConfig() }

    member __.Run(state: FunctionState) : (HttpRequest -> Task<IActionResult>) =
      let wrapGiraffeServices (existing:IServiceProvider) =
        { new IServiceProvider with
            member __.GetService(t:Type) =
              if (t = typeof<Json.ISerializer>) then upcast state.JsonSerializer
              elif (t = typeof<Xml.ISerializer>) then upcast state.XmlSerializer
              elif (t = typeof<INegotiationConfig>) then upcast state.NegotiationConfig
              else existing.GetService(t)
        }

      let next : HttpContext -> Task<HttpContext option> = Some >> Task.FromResult
      fun req ->
        req.HttpContext.RequestServices <- wrapGiraffeServices req.HttpContext.RequestServices
        let r =
          match state.Router with
          | Some r -> r
          | None -> failwith "Router needs to be defined"
        let r = router {
          pipe_through (fun nxt ctx -> state.Logger |> Option.iter (fun log -> ctx.Items.["Logger"] <- log); nxt ctx)
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
              | Some _ -> return mvcNoopResult
            | Some _ -> return mvcNoopResult
          with
          | ex ->
            let! errorResult = state.ErrorHandler ex next req.HttpContext
            match errorResult with
            | None -> return failwith "Internal error"
            | Some _ -> return mvcNoopResult
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

        ///Configures built in JSON.Net (de)serializer with custom settings.
    [<CustomOperation("use_json_settings")>]
    member __.ConfigJSONSerializer (state, settings) =
      { state with JsonSerializer = NewtonsoftJson.Serializer settings }

    ///Replaces built in JSON.Net (de)serializer with custom serializer
    [<CustomOperation("use_json_serializer")>]
    member __.UseCustomJSONSerializer (state, serializer : #Json.ISerializer ) =
      { state with JsonSerializer = serializer }

    ///Configures built in XML (de)serializer with custom settings.
    [<CustomOperation("use_xml_settings")>]
    member __.ConfigXMLSerializer (state, settings) =
      { state with XmlSerializer = SystemXml.Serializer settings }

    ///Replaces built in XML (de)serializer with custom serializer
    [<CustomOperation("use_xml_serializer")>]
    member __.UseCustomXMLSerializer (state, serializer : #Xml.ISerializer ) =
      { state with XmlSerializer = serializer }

    ///Configures negotiation config
    [<CustomOperation("use_negotiation_config")>]
    member __.UseConfigNegotiation (state, config) =
      { state with NegotiationConfig = config }


  let azureFunction = FunctionBuilder()
