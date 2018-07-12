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
open Microsoft.AspNetCore.Rewrite
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authorization
open Microsoft.AspNetCore.StaticFiles

[<AutoOpen>]
module Application =
  type ApplicationState = {
    Router: HttpHandler option
    ErrorHandler: ErrorHandler option
    Pipelines: HttpHandler list
    Urls: string list
    MimeTypes: (string*string) list
    AppConfigs: (IApplicationBuilder -> IApplicationBuilder) list
    HostConfigs: (IWebHostBuilder -> IWebHostBuilder) list
    ServicesConfig: (IServiceCollection -> IServiceCollection) list
    CliArguments: string array option
    CookiesAlreadyAdded: bool
  }

  type ApplicationBuilder internal () =
    member __.Yield(_) =
      let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> Giraffe.HttpStatusCodeHandlers.ServerErrors.INTERNAL_ERROR ex.Message


      {Router = None; ErrorHandler = Some errorHandler; Pipelines = []; Urls = []; MimeTypes = []; AppConfigs = []; HostConfigs = []; ServicesConfig = []; CliArguments = None; CookiesAlreadyAdded = false }

    member __.Run(state: ApplicationState) : IWebHostBuilder =
      match state.Router with
      | None -> failwith "Router needs to be defined in Saturn application"
      | Some router ->
      let router = (succeed |> List.foldBack (fun e acc -> acc >=> e) state.Pipelines) >=> router

      let appConfigs (app: IApplicationBuilder) =
        let app = app |> List.foldBack(fun e acc -> e acc) state.AppConfigs
        let app =
          match state.ErrorHandler with
          | Some err -> app.UseGiraffeErrorHandler(err)
          | None -> app
        app.UseGiraffe router

      let serviceConfigs (services : IServiceCollection) =
        let services = services.AddGiraffe()
        state.ServicesConfig |> List.rev |> List.iter (fun fn -> fn services |> ignore)

      let wbhst =
        // Explicit null removes unnecessary handlers.
        WebHost.CreateDefaultBuilder(Option.toObj state.CliArguments)
        |> List.foldBack (fun e acc -> e acc ) state.HostConfigs
      wbhst
        .Configure(Action<IApplicationBuilder> appConfigs)
        .ConfigureServices(Action<IServiceCollection> serviceConfigs)
        .UseUrls(state.Urls |> List.toArray)

    ///Defines top-level router used for the application
    ///This construct is obsolete, use `use_router` instead
    [<CustomOperation("router")>]
    [<ObsoleteAttribute("This construct is obsolete, use use_router instead")>]
    member __.RouterOld(state, handler) =
      {state with Router = Some handler}

    ///Defines top-level router used for the application
    [<CustomOperation("use_router")>]
    [<ObsoleteAttribute("This construct is obsolete, use use_router instead")>]
    member __.Router(state, handler) =
      {state with Router = Some handler}

    ///Adds pipeline to the list of pipelines that will be used for every request
    [<CustomOperation("pipe_through")>]
    member __.PipeThrough(state : ApplicationState, pipe) =
      {state with Pipelines = pipe::state.Pipelines}

    ///Adds error/not-found handler for current scope
    [<CustomOperation("error_handler")>]
    member __.ErrprHandler(state : ApplicationState, handler) =
      {state with ErrorHandler = Some handler}

    ///Adds custom application configuration step.
    [<CustomOperation("app_config")>]
    member __.AppConfig(state, config) =
      {state with AppConfigs = config::state.AppConfigs}

    ///Adds custom host configuration step.
    [<CustomOperation("host_config")>]
    member __.HostConfig(state, config) =
      {state with HostConfigs = config::state.HostConfigs}

    ///Adds custom service configuration step.
    [<CustomOperation("service_config")>]
    member __.ServiceConfig(state, config) =
      {state with ServicesConfig = config::state.ServicesConfig}

    ///Adds url
    [<CustomOperation("url")>]
    member __.Url(state, url) =
      {state with Urls = url::state.Urls}

    ///Adds MIME types definitions as a list of (extension, mime)
    [<CustomOperation("use_mime_types")>]
    member __.AddMimeTypes(state, mimeList) =
      {state with MimeTypes = mimeList}


    ///Adds logging configuration.
    [<CustomOperation("logging")>]
    member __.Logging(state, (config : ILoggingBuilder -> unit)) =
      {state with HostConfigs = (fun (app : IWebHostBuilder)-> app.ConfigureLogging(config))::state.HostConfigs}

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
    ///Enables using static file hosting.
    [<CustomOperation("use_static")>]
    member __.UseStatic(state, path : string) =
      let middleware (app : IApplicationBuilder) =
        match app.UseDefaultFiles(), state.MimeTypes with
        |app, [] -> app.UseStaticFiles()
        |app, mimes ->
            let provider = FileExtensionContentTypeProvider()
            mimes |> List.iter (fun (extension, mime) -> provider.Mappings.[extension] <- mime)
            app.UseStaticFiles(StaticFileOptions(ContentTypeProvider=provider))
      let host (builder: IWebHostBuilder) =
        let p = Path.Combine(Directory.GetCurrentDirectory(), path)
        builder
          .UseWebRoot(p)
      { state with
          AppConfigs = middleware::state.AppConfigs
          HostConfigs = host::state.HostConfigs
      }

    [<CustomOperation("use_config")>]
    member __.UseConfig(state : ApplicationState, configBuilder : unit -> 'a) =
      let x = lazy(configBuilder ())
      let handler (nxt : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) : HttpFuncResult =
        ctx.Items.["Configuration"] <- x.Value
        nxt ctx

      {state with Pipelines = handler::state.Pipelines}

    ///Redirect all HTTP request to HTTPS
    [<CustomOperation("force_ssl")>]
    member __.ForceSSL(state : ApplicationState) =
      let middleware (app : IApplicationBuilder) =
        let opts = RewriteOptions().AddRedirectToHttps()
        app.UseRewriter opts

      {state with AppConfigs=middleware::state.AppConfigs}

    ///Enables application level CORS protection
    [<CustomOperation("use_cors")>]
    member __.UseCors(state: ApplicationState, policy : string, (policyConfig : CorsPolicyBuilder -> unit ) ) =
      let service (s : IServiceCollection) =
        s.AddCors(fun o -> o.AddPolicy(policy, policyConfig) |> ignore)
      let middleware (app : IApplicationBuilder) =
        app.UseCors policy

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
      }

    ///Enables default JWT authentication
    [<CustomOperation("use_jwt_authentication")>]
    member __.UseJWTAuth(state: ApplicationState, secret: string, issuer : string) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- JwtBearerDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
         .AddJwtBearer(fun opt ->
            let tvp = TokenValidationParameters()
            tvp.ValidateActor <- true
            tvp.ValidateAudience <- true
            tvp.ValidateLifetime <- true
            tvp.ValidateIssuerSigningKey <- true
            tvp.ValidIssuer <- issuer
            tvp.ValidAudience <- issuer
            tvp.IssuerSigningKey <- SymmetricSecurityKey(Text.Encoding.UTF8.GetBytes secret)
            opt.TokenValidationParameters <- tvp
         ) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
      }

    ///Enables JWT authentication with custom configuration
    [<CustomOperation("use_jwt_authentication_with_config")>]
    member __.UseJWTAuthConfig(state: ApplicationState, (config : JwtBearerOptions -> unit)) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- JwtBearerDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
         .AddJwtBearer(Action<JwtBearerOptions> config) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
      }

    ///Enables default cookies authentication
    [<CustomOperation("use_cookies_authentication")>]
    member __.UseCookiesAuth(state: ApplicationState, issuer : string) =
      let mutable flag = state.CookiesAlreadyAdded
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- CookieAuthenticationDefaults.AuthenticationScheme)
        if not flag then
          flag <- true
          c.AddCookie(fun opt ->
            opt.ClaimsIssuer <- issuer
          ) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = flag
      }

    ///Enables cookies authentication with custom configuration
    [<CustomOperation("use_cookies_authentication_with_config")>]
    member __.UseCookiesAuthConfig(state: ApplicationState, (options :  CookieAuthenticationOptions -> unit) ) =
      let mutable flag = state.CookiesAlreadyAdded

      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- CookieAuthenticationDefaults.AuthenticationScheme)
        if not flag then
          flag <- true
          c.AddCookie(options) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = flag
      }

    ///Enables custom OAuth authentication
    [<CustomOperation("use_custom_oauth")>]
    member __.UseCustomOAuth(state: ApplicationState, name : string, (config : Authentication.OAuth.OAuthOptions -> unit) ) =
      let mutable flag = state.CookiesAlreadyAdded
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- name)
        if not flag then
          flag <- true
          c.AddCookie() |> ignore
        c.AddOAuth(name,config) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = flag
      }

    ///Enables IIS integration
    [<CustomOperation("use_iis")>]
    member __.UseIIS(state) =
      let host (builder: IWebHostBuilder) =
        builder.UseIISIntegration()
      { state with
          HostConfigs = host::state.HostConfigs
      }

    ///Add custom policy, taking an `AuthorizationHandlerContext -> bool`
    [<CustomOperation("use_policy")>]
    member __.UsePolicy(state, policy, evaluator: AuthorizationHandlerContext -> bool) =
      let policyBuilder (opt : AuthorizationOptions) =
        opt.AddPolicy(policy,
          Action<AuthorizationPolicyBuilder>
            (fun builder -> builder.RequireAssertion evaluator |> ignore))
      let service (s : IServiceCollection) =
        s.AddAuthorization (Action<AuthorizationOptions> policyBuilder)
      { state with
          ServicesConfig = service::state.ServicesConfig
      }

    ///Disables generation of diagnostic files that can be used by Saturn tooling.
    [<CustomOperation("disable_diagnostics")>]
    member __.DisableDiagnostics (state) =
      SiteMap.isDebug <- false
      state

    ///Enables use of the `protectFromForgery` `pipeline` component and the `CSRF` features in general.
    [<CustomOperation("use_antiforgery")>]
    member __.UseAntiforgery (state) =
      let antiforgeryService (s: IServiceCollection) =
        s.AddAntiforgery()
      { state with
          ServicesConfig = antiforgeryService :: state.ServicesConfig }

    ///Enables use of the `protectFromForgery` `pipeline` component and the `CSRF` features in general.
    ///This overload allows for custom configuration of the subsystem, for more information see the `AntiforgeryOptions` class at https://github.com/aspnet/Antiforgery/blob/dev/src/Microsoft.AspNetCore.Antiforgery/AntiforgeryOptions.cs
    [<CustomOperation("use_antiforgery_with_config")>]
    member __.UseAntiforgeryWithConfig (state, configFn) =
      let antiforgeryService (s: IServiceCollection) =
        s.AddAntiforgery(Action<_>configFn)
      { state with
          ServicesConfig = antiforgeryService :: state.ServicesConfig }

    ///Sets the cli arguments for the `IWebHostBuilder` to enable default command line configuration and functionality.
    [<CustomOperation("cli_arguments")>]
    member __.CliArguments (state, args) =
      { state with
          CliArguments = Some args
      }

    ///Configures built in JSON.Net (de)serializer with custom settings.
    [<CustomOperation("use_json_settings")>]
    member __.ConfigJSONSerializer (state, settings) =
      let jsonSettingsService (s: IServiceCollection) =
        s.AddSingleton<Giraffe.Serialization.Json.IJsonSerializer>(Giraffe.Serialization.Json.NewtonsoftJsonSerializer settings)
      { state with
          ServicesConfig = jsonSettingsService :: state.ServicesConfig }

    ///Replaces built in JSON.Net (de)serializer with custom serializer
    [<CustomOperation("use_json_serializer")>]
    member __.UseCustomJSONSerializer (state, serializer : #Giraffe.Serialization.Json.IJsonSerializer ) =
      let jsonService (s: IServiceCollection) =
        s.AddSingleton<Giraffe.Serialization.Json.IJsonSerializer>(serializer)
      { state with
          ServicesConfig = jsonService :: state.ServicesConfig }

    ///Configures built in XML (de)serializer with custom settings.
    [<CustomOperation("use_xml_settings")>]
    member __.ConfigXMLSerializer (state, settings) =
      let xmlService (s: IServiceCollection) =
        s.AddSingleton<Giraffe.Serialization.Xml.IXmlSerializer>(Giraffe.Serialization.Xml.DefaultXmlSerializer settings)
      { state with
          ServicesConfig = xmlService :: state.ServicesConfig }

    ///Replaces built in XML (de)serializer with custom serializer
    [<CustomOperation("use_xml_serializer")>]
    member __.UseCustomXMLSerializer (state, serializer : #Giraffe.Serialization.Xml.IXmlSerializer ) =
      let xmlService (s: IServiceCollection) =
        s.AddSingleton<Giraffe.Serialization.Xml.IXmlSerializer>(serializer)
      { state with
          ServicesConfig = xmlService :: state.ServicesConfig }

  ///Computation expression used to configure Saturn application
  let application = ApplicationBuilder()

  ///Runs Saturn application
  let run (app: IWebHostBuilder) =
    if SiteMap.isDebug then SiteMap.generate ()
    app.Build().Run()
