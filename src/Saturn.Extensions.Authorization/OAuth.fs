module Saturn

open Saturn
open Giraffe
open Microsoft.AspNetCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open System.Threading.Tasks
open System
open Microsoft.AspNetCore.Authentication.Google
open Microsoft.AspNetCore.Authentication
open System.Net.Http
open System.Net.Http.Headers
open Newtonsoft.Json.Linq

type ApplicationBuilder with
    ///Enables default Google OAuth authentication.
    ///`jsonToClaimMap` should contain sequance of tuples where first element is a name of the of the key in JSON object and second element is a name of the claim.
    ///For example: `["id", ClaimTypes.NameIdentifier; "displayName", ClaimTypes.Name]` where `id` and `displayName` are names of fields in the Google JSON response (https://developers.google.com/+/web/api/rest/latest/people#resource).
    [<CustomOperation("use_google_oauth")>]
    member __.UseGoogleAuth(state: ApplicationState, clientId : string, clientSecret : string, callbackPath : string, jsonToClaimMap : (string * string) seq) =
      let mutable flag = state.CookiesAlreadyAdded
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "Google")
        if not flag then
          flag <- true
          c.AddCookie() |> ignore
        c.AddGoogle(fun opt ->
        opt.ClientId <- clientId
        opt.ClientSecret <- clientSecret
        opt.CallbackPath <- PathString(callbackPath)
        jsonToClaimMap |> Seq.iter (fun (k,v) -> opt.ClaimActions.MapJsonKey(v,k) )
        opt.ClaimActions.MapJsonSubKey("urn:google:image:url", "image", "url")
        let ev = opt.Events

        ev.OnCreatingTicket <-
          fun ctx ->
            let tsk = task {
              let req = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint)
              req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
              req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", ctx.AccessToken)
              let! (response : HttpResponseMessage) = ctx.Backchannel.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.HttpContext.RequestAborted)
              response.EnsureSuccessStatusCode () |> ignore
              let! cnt = response.Content.ReadAsStringAsync()
              let user = JObject.Parse cnt
              ctx.RunClaimActions user
            }
            Task.Factory.StartNew(fun () -> tsk.Result)

        ) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = flag
      }

    ///Enables Google OAuth authentication with custom configuration
    [<CustomOperation("use_google_oauth_with_config")>]
    member __.UseGoogleAuthWithConfig(state: ApplicationState, (config : Authentication.Google.GoogleOptions -> unit) ) =
      let mutable flag = state.CookiesAlreadyAdded
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "Google")
        if not flag then
          flag <- true
          c.AddCookie() |> ignore
        c.AddGoogle(Action<GoogleOptions> config) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = flag
      }

    ///Enables default GitHub OAuth authentication.
    ///`jsonToClaimMap` should contain sequance of tuples where first element is a name of the of the key in JSON object and second element is a name of the claim.
    ///For example: `["login", "githubUsername"; "name", "fullName"]` where `login` and `name` are names of fields in GitHub JSON response (https://developer.github.com/v3/users/#get-the-authenticated-user).
    [<CustomOperation("use_github_oauth")>]
    member __.UseGithubAuth(state: ApplicationState, clientId : string, clientSecret : string, callbackPath : string, jsonToClaimMap : (string * string) seq) =
      let mutable flag = state.CookiesAlreadyAdded
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "GitHub")
        if not flag then
          flag <- true
          c.AddCookie() |> ignore
        c.AddOAuth("GitHub", fun (opt : Authentication.OAuth.OAuthOptions) ->
          opt.ClientId <- clientId
          opt.ClientSecret <- clientSecret
          opt.CallbackPath <- PathString(callbackPath)
          opt.AuthorizationEndpoint <-  "https://github.com/login/oauth/authorize"
          opt.TokenEndpoint <- "https://github.com/login/oauth/access_token"
          opt.UserInformationEndpoint <- "https://api.github.com/user"
          jsonToClaimMap |> Seq.iter (fun (k,v) -> opt.ClaimActions.MapJsonKey(v,k) )
          let ev = opt.Events

          ev.OnCreatingTicket <-
            fun ctx ->
              let tsk = task {
                let req = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint)
                req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
                req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", ctx.AccessToken)
                let! (response : HttpResponseMessage) = ctx.Backchannel.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ctx.HttpContext.RequestAborted)
                response.EnsureSuccessStatusCode () |> ignore
                let! cnt = response.Content.ReadAsStringAsync()
                let user = JObject.Parse cnt
                ctx.RunClaimActions user
              }
              Task.Factory.StartNew(fun () -> tsk.Result)

         ) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = flag
      }

    ///Enables GitHub OAuth authentication with custom configuration
    [<CustomOperation("use_github_oauth_with_config")>]
    member __.UseGithubAuthWithConfig(state: ApplicationState, (config : Authentication.OAuth.OAuthOptions -> unit) ) =
      let mutable flag = state.CookiesAlreadyAdded
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "GitHub")
        if not flag then
          flag <- true
          c.AddCookie() |> ignore
        c.AddOAuth("GitHub",config) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = flag
      }

