namespace Saturn

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.ResponseCompression
open Giraffe
open Microsoft.AspNetCore
open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System.IO

type ApplicationState = {
  Router: HttpHandler option
  ErrorHandler: ErrorHandler option
  Pipelines: HttpHandler list
  Urls: string list
  AppConfigs: (IApplicationBuilder -> IApplicationBuilder) list
  HostConfigs: (IWebHostBuilder -> IWebHostBuilder) list
  ServicesConfig: (IServiceCollection -> IServiceCollection) list
}

module Application =
  type ApplicationBuilder internal () =
    member __.Yield(_) =
      let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> Giraffe.HttpStatusCodeHandlers.ServerErrors.INTERNAL_ERROR ex.Message


      {Router = None; ErrorHandler = Some errorHandler; Pipelines = []; Urls = []; AppConfigs = []; HostConfigs = []; ServicesConfig = [] }

    member __.Run(state: ApplicationState) : IWebHost =
      match state.Router with
      | None -> failwith "Router needs to be defined in Saturn application"
      | Some router ->
      let router = (succeed |> List.foldBack (>=>) state.Pipelines) >=> router

      let appConfigs (app: IApplicationBuilder) =
        let app = app |> List.foldBack(fun e acc -> e acc) state.AppConfigs
        let app =
          match state.ErrorHandler with
          | Some err -> app.UseGiraffeErrorHandler(err)
          | None -> app
        app.UseGiraffe router

      let serviceConfigs (services : IServiceCollection) =
        state.ServicesConfig |> List.rev |> List.iter (fun fn -> fn services |> ignore)

      let wbhst = WebHost.CreateDefaultBuilder() |> List.foldBack (fun e acc -> e acc ) state.HostConfigs
      wbhst
        .Configure(Action<IApplicationBuilder> appConfigs)
        .ConfigureServices(Action<IServiceCollection> serviceConfigs)
        .UseUrls(state.Urls |> List.toArray)
        .Build()

    ///Defines top-level router used for the application
    [<CustomOperation("router")>]
    member __.Router(state, handler) =
      {state with Router = Some handler}

    ///Adds pipeline to the list of pipelines that will be used for every request
    [<CustomOperation("pipe_through")>]
    member __.PipeThrough(state, pipe) =
      {state with Pipelines = pipe::state.Pipelines}

    ///Adds error/not-found handler for current scope
    [<CustomOperation("error_handler")>]
    member __.ErrprHandler(state, handler) =
      {state with ErrorHandler = Some handler}

    ///Adds custom application configuration step.
    [<CustomOperation("app_config")>]
    member __.AppConfig(state, config) =
      {state with AppConfigs = config::state.AppConfigs}

    ///Adds custom host configuration step.
    [<CustomOperation("host_config")>]
    member __.HostConfig(state, config) =
      {state with HostConfigs = config::state.HostConfigs}

    ///Adds url
    [<CustomOperation("url")>]
    member __.Url(state, url) =
      {state with Urls = url::state.Urls}

    ///Enables in-memory session cache
    [<CustomOperation("memory_cache")>]
    member __.MemoryCache(state) =
      let service (s : IServiceCollection) = s.AddDistributedMemoryCache()
      let serviceSet (s : IServiceCollection) = s.AddSession()

      { state with
          ServicesConfig = serviceSet::(service::state.ServicesConfig)
          AppConfigs = (fun (app : IApplicationBuilder)-> app.UseSession())::state.AppConfigs
      }

    ///Enables gzip compression
    [<CustomOperation("use_gzip")>]
    member __.UseGZip(state : ApplicationState) =
      let service (s : IServiceCollection) =
        s.Configure<GzipCompressionProviderOptions>(fun (opts : GzipCompressionProviderOptions) -> opts.Level <- System.IO.Compression.CompressionLevel.Optimal)
         .AddResponseCompression()
      let middleware (app : IApplicationBuilder) = app.UseResponseCompression()

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
      }

    [<CustomOperation("use_static")>]
    member __.UseStatic(state, path : string) =
      let middleware (app : IApplicationBuilder) = app.UseStaticFiles()
      let host (builder: IWebHostBuilder) =
        let p = Path.Combine(Directory.GetCurrentDirectory(), path)
        builder.UseWebRoot p
      { state with
          AppConfigs = middleware::state.AppConfigs
          HostConfigs = host::state.HostConfigs
      }

    [<CustomOperation("use_config")>]
    member __.UseConfig(state, configBuilder : unit -> 'a) =
      let x = lazy(configBuilder ())
      let handler (nxt : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) : HttpFuncResult =
        ctx.Items.["Configuration"] <- x.Value
        nxt ctx

      {state with Pipelines = state.Pipelines}

  let application = ApplicationBuilder()

  let run (app: IWebHost) = app.Run()