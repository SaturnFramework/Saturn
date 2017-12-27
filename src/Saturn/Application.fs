namespace Saturn

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Giraffe
open Microsoft.AspNetCore
open System
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Session

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
      {Router = None; ErrorHandler = None; Pipelines = []; Urls = []; AppConfigs = []; HostConfigs = []; ServicesConfig = [] }

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

  let application = ApplicationBuilder()