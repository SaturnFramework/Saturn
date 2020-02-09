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
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Authentication
open FSharp.Control.Tasks.V2
open System.Net.Http
open System.Net.Http.Headers
open Newtonsoft.Json.Linq
open System.Threading.Tasks
open Channels
open Giraffe.Serialization.Json

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
    NoRouter: bool
    Channels: (string * IChannel) list
  }

  let private addCookie state (c : AuthenticationBuilder) = if not state.CookiesAlreadyAdded then c.AddCookie() |> ignore
  // generic oauth parse and validate logic, shared with the auth extensions package
  let parseAndValidateOauthTicket =
    fun (ctx: OAuth.OAuthCreatingTicketContext) ->
      let tsk = task {
        let req = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint)
        req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
        req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", ctx.AccessToken)
        let! (response : HttpResponseMessage) = ctx.Backchannel.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.HttpContext.RequestAborted)
        response.EnsureSuccessStatusCode () |> ignore
#if NETSTANDARD2_0
        let! cnt = response.Content.ReadAsStringAsync()
        let user = JObject.Parse cnt
#endif
#if NETCOREAPP3_1
        let! responseStream = response.Content.ReadAsStreamAsync()
        let! user = System.Text.Json.JsonSerializer.DeserializeAsync(responseStream)
#endif
        ctx.RunClaimActions user
      }
      Task.Factory.StartNew(fun () -> tsk.Result)

  type ApplicationBuilder internal () =
    member __.Yield(_) =
      let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> Giraffe.HttpStatusCodeHandlers.ServerErrors.INTERNAL_ERROR ex.Message
      {Router = None; ErrorHandler = Some errorHandler; Pipelines = []; Urls = []; MimeTypes = []; AppConfigs = []; HostConfigs = []; ServicesConfig = []; CliArguments = None; CookiesAlreadyAdded = false; NoRouter = false; Channels = [] }

    member __.Run(state: ApplicationState) : IWebHostBuilder =
      /// to build the app we have to separate our configurations and our pipelines.
      /// we can only call `Configure` once, so we have to apply our pipelines in the end
      let router =
        match state.Router with
        | None ->
          if not state.NoRouter then printfn "Router needs to be defined in Saturn application. If you're building channels-only application, or gRPC application you may disable this message with `no_router` flag in your `application` block"
          None
        | Some router ->
          Some ((succeed |> List.foldBack (fun e acc -> acc >=> e) state.Pipelines) >=> router)

      // as we want to add middleware to our pipeline, we can add it here and we'll fold across it in the end
      let useParts = ResizeArray<IApplicationBuilder -> IApplicationBuilder>()

      let wbhst =
        // Explicit null removes unnecessary handlers.
        WebHost.CreateDefaultBuilder(Option.toObj state.CliArguments)
        |> List.foldBack (fun e acc -> e acc ) state.HostConfigs

      wbhst.ConfigureServices(fun svcs ->
        let services = svcs.AddGiraffe()
        state.ServicesConfig |> List.rev |> List.iter (fun fn -> fn services |> ignore) |> ignore)
      |> ignore // need giraffe (with user customizations) in place so that I can get an IJsonSerializer for the channels

      /// error handler first so that errors are caught
      match state.ErrorHandler with
      | Some err -> useParts.Add (fun app -> app.UseGiraffeErrorHandler(err))
      | None -> ()

      /// channels next so that they don't get swallowed by Giraffe
      match state.Channels with
      | [] -> ()
      | channels ->
        // we have to build the provider so we can get a serializer so we can make a singleton instance of the hub to register
        // as both the interface _and_ itself, so that users can use `ISocketHub` without getting the add/remove socket members
        wbhst.ConfigureServices(fun svcs ->
          let provider = svcs.BuildServiceProvider()
          let serializer = provider.GetRequiredService(typeof<IJsonSerializer>) :?> IJsonSerializer
          let hub = Channels.SocketHub(serializer)
          svcs
            .AddSingleton<Channels.ISocketHub>(hub)
            .AddSingleton<Channels.SocketHub>(hub)
            |> ignore
        ) |> ignore

        useParts.Add(fun (ab: IApplicationBuilder) -> ab.UseWebSockets())
        channels
        |> List.iter (fun (url, chnl) -> useParts.Add (fun ab -> ab.UseMiddleware<SocketMiddleware>(url, chnl)))

      /// user-provided middleware
      state.AppConfigs |> List.iter (useParts.Add)

      /// finally Giraffe itself
      match router with
      | None -> ()
      | Some router -> useParts.Add (fun app -> app.UseGiraffe router; app)

      let wbhst =
        if not (state.Urls |> List.isEmpty) then
          wbhst.UseUrls(state.Urls |> List.toArray)
        else wbhst

      wbhst.Configure(fun ab ->
        (ab, useParts)
        ||> Seq.fold (fun ab part -> part ab)
        |> ignore
      )

    ///Defines top-level router used for the application
    ///This construct is obsolete, use `use_router` instead
    [<CustomOperation("router")>]
    [<ObsoleteAttribute("This construct is obsolete, use use_router instead")>]
    member __.RouterOld(state, handler) =
      {state with Router = Some handler}

    ///Defines top-level router used for the application
    [<CustomOperation("use_router")>]
    member __.Router(state, handler) =
      {state with Router = Some handler}

    ///Disable warning message about lack of `router` definition. Should be used for channels-only or gRPC applications.
    [<CustomOperation("no_router")>]
    member __.NoRouter(state) =
      {state with NoRouter = true}

    ///Adds pipeline to the list of pipelines that will be used for every request
    [<CustomOperation("pipe_through")>]
    member __.PipeThrough(state : ApplicationState, pipe) =
      {state with Pipelines = pipe::state.Pipelines}

    ///Adds error/not-found handler for current scope
    [<CustomOperation("error_handler")>]
    member __.ErrorHandler(state : ApplicationState, handler) =
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
    member __.UseConfig(state : ApplicationState, configBuilder : IConfiguration -> 'a) =
      let mutable (x: 'a option) = None
      let handler (nxt : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) : HttpFuncResult =
        let v =
          match x with
          | None ->
            let ic = ctx.GetService<IConfiguration>()
            let v = configBuilder ic
            x <- Some v
            v
          | Some v -> v

        ctx.Items.["Configuration"] <- v
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

    ///Enables simple custom OAuth authentication using parmeters provided with `OAuth.OAuthSettings` record.
    ///Can be used to quickly implement default OAuth authentication for 3rd party providers.
    [<CustomOperation("use_oauth")>]
    member __.UseOAuthWithSettings(state: ApplicationState,  clientId : string, clientSecret : string, settings: Saturn.OAuth.OAuthSettings) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- settings.Schema)
        addCookie state c
        c.AddOAuth(settings.Schema, fun (opt : Authentication.OAuth.OAuthOptions) ->
          opt.ClientId <- clientId
          opt.ClientSecret <- clientSecret
          opt.CallbackPath <- PathString(settings.CallbackPath)
          opt.AuthorizationEndpoint <- settings.AuthorizationEndpoint
          opt.TokenEndpoint <- settings.TokenEndpoint
          opt.UserInformationEndpoint <- settings.UserInformationEndpoint
          settings.Claims |> Seq.iter (fun (k,v) -> opt.ClaimActions.MapJsonKey(v,k) )
          let ev = opt.Events

          ev.OnCreatingTicket <-
            fun ctx ->
              let tsk = task {
                let req = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint)
                req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
                req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", ctx.AccessToken)
                let! (response : HttpResponseMessage) = ctx.Backchannel.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.HttpContext.RequestAborted)
                response.EnsureSuccessStatusCode () |> ignore
#if NETSTANDARD2_0
                let! cnt = response.Content.ReadAsStringAsync()
                let user = JObject.Parse cnt
#endif
#if NETCOREAPP3_1
                let! responseStream = response.Content.ReadAsStreamAsync()
                let! user = System.Text.Json.JsonSerializer.DeserializeAsync(responseStream)
#endif
                ctx.RunClaimActions user
              }
              Task.Factory.StartNew(fun () -> tsk.Result)

         ) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = true
      }

    ///Enables custom OAuth authentication
    [<CustomOperation("use_custom_oauth")>]
    [<ObsoleteAttribute("This construct is obsolete, use `use_oauth_with_config` instead")>]
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

    ///Enables OAuth authentication with custom configuration
    [<CustomOperation("use_oauth_with_config")>]
    member __.UseOAuthWithConfig(state: ApplicationState, name : string, (config : Authentication.OAuth.OAuthOptions -> unit) ) =
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
#if NETSTANDARD2_0
        s.AddAuthorization (Action<AuthorizationOptions> policyBuilder)
#endif
#if NETCOREAPP3_1
        s.AddAuthorizationCore (Action<AuthorizationOptions> policyBuilder)
#endif
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

    ///Registers channel for given url.
    [<CustomOperation("add_channel")>]
    member __.AddChannel (state, url: string, channel: IChannel ) =
      { state with
          Channels = (url, channel) :: state.Channels }

    /// Turns on the developer exception page, if the environment is in development mode.
    [<CustomOperation "use_developer_exceptions">]
    member __.ActivateDeveloperExceptions (state: ApplicationState) =
        let config (app:IApplicationBuilder) (env:IHostingEnvironment) =
            if env.IsDevelopment() then app.UseDeveloperExceptionPage()
            else app

        let middleware (app:IApplicationBuilder) =
          let env = app.ApplicationServices.GetService<IHostingEnvironment>()
          config app env

        {state with AppConfigs=middleware::state.AppConfigs}

    /// Listens on `::1` and `127.0.0.1` with the given port. Requesting a dynamic port by specifying `0` is not supported for this type of endpoint
    [<CustomOperation "listen_local">]
    member __.ListenLocal (state:ApplicationState, portNumber, listenOptions : Server.Kestrel.Core.ListenOptions -> unit) =
        let config (webHostBuilder:IWebHostBuilder) =
            webHostBuilder
               .ConfigureKestrel(fun options -> options.ListenLocalhost(portNumber, Action<Server.Kestrel.Core.ListenOptions> listenOptions))

        {state with HostConfigs = config::state.HostConfigs}

  ///Computation expression used to configure Saturn application
  let application = ApplicationBuilder()

  ///Runs Saturn application
  let run (app: IWebHostBuilder) =
    if SiteMap.isDebug then SiteMap.generate ()
    app.Build().Run()
