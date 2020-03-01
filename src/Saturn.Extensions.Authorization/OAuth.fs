module Saturn

open Saturn
open FSharp.Control.Tasks.V2.ContextInsensitive
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

let private addCookie state (c : AuthenticationBuilder) = if not state.CookiesAlreadyAdded then c.AddCookie() |> ignore

type Saturn.Application.ApplicationBuilder with
    ///Enables default Google OAuth authentication.
    ///`jsonToClaimMap` should contain sequance of tuples where first element is a name of the of the key in JSON object and second element is a name of the claim.
    ///For example: `["id", ClaimTypes.NameIdentifier; "displayName", ClaimTypes.Name]` where `id` and `displayName` are names of fields in the Google JSON response (https://developers.google.com/+/web/api/rest/latest/people#resource).
    [<CustomOperation("use_google_oauth")>]
    member __.UseGoogleAuth(state: ApplicationState, clientId : string, clientSecret : string, callbackPath : string, jsonToClaimMap : (string * string) seq) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "Google")
        addCookie state c
        c.AddGoogle(fun opt ->
        opt.ClientId <- clientId
        opt.ClientSecret <- clientSecret
        opt.CallbackPath <- PathString(callbackPath)
        jsonToClaimMap |> Seq.iter (fun (k,v) -> opt.ClaimActions.MapJsonKey(v,k) )
        opt.ClaimActions.MapJsonSubKey("urn:google:image:url", "image", "url")
        let ev = opt.Events

        ev.OnCreatingTicket <- Func<_,_> Saturn.Application.parseAndValidateOauthTicket

        ) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = true
      }

    ///Enables Google OAuth authentication with custom configuration
    [<CustomOperation("use_google_oauth_with_config")>]
    member __.UseGoogleAuthWithConfig(state: ApplicationState, (config : Authentication.Google.GoogleOptions -> unit) ) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "Google")
        addCookie state c
        c.AddGoogle(Action<GoogleOptions> config) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = true
      }

    ///Enables default GitHub OAuth authentication.
    ///`jsonToClaimMap` should contain sequance of tuples where first element is a name of the of the key in JSON object and second element is a name of the claim.
    ///For example: `["login", "githubUsername"; "name", "fullName"]` where `login` and `name` are names of fields in GitHub JSON response (https://developer.github.com/v3/users/#get-the-authenticated-user).
    [<CustomOperation("use_github_oauth")>]
    member __.UseGithubAuth(state: ApplicationState, clientId : string, clientSecret : string, callbackPath : string, jsonToClaimMap : (string * string) seq) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "GitHub")
        addCookie state c
        c.AddOAuth("GitHub", fun (opt : Authentication.OAuth.OAuthOptions) ->
          opt.ClientId <- clientId
          opt.ClientSecret <- clientSecret
          opt.CallbackPath <- PathString(callbackPath)
          opt.AuthorizationEndpoint <-  "https://github.com/login/oauth/authorize"
          opt.TokenEndpoint <- "https://github.com/login/oauth/access_token"
          opt.UserInformationEndpoint <- "https://api.github.com/user"
          jsonToClaimMap |> Seq.iter (fun (k,v) -> opt.ClaimActions.MapJsonKey(v,k) )
          let ev = opt.Events

          ev.OnCreatingTicket <- Func<_,_> Saturn.Application.parseAndValidateOauthTicket

         ) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = true
      }

    ///Enables GitHub OAuth authentication with custom configuration
    [<CustomOperation("use_github_oauth_with_config")>]
    member __.UseGithubAuthWithConfig(state: ApplicationState, (config : Authentication.OAuth.OAuthOptions -> unit) ) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "GitHub")
        addCookie state c
        c.AddOAuth("GitHub",config) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = true
      }

    ///Enalbes default Azure AD OAuth authentication.
    ///`scopes` must be at least on of the scopes defined in https://docs.microsoft.com/en-us/graph/permissions-reference, for instance "User.Read".
    ///`jsonToClaimMap` should contain sequance of tuples where first element is a name of the of the key in JSON object and second element is a name of the claim.
    ///For example: `["name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" ]` where `name` is the names of a field in Azure AD's JSON response (see https://docs.microsoft.com/en-us/azure/active-directory/develop/id-tokens or inspect tokens with https://jwt.ms).
    [<CustomOperation("use_azuread_oauth")>]
    member __.UseAzureADAuth(state: ApplicationState, tenantId : string, clientId : string, clientSecret: string, callbackPath : string, scopes : string seq, jsonToClaimMap : (string * string) seq) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "AzureAD")
        addCookie state c
        c.AddOAuth("AzureAD", fun (opt : Authentication.OAuth.OAuthOptions) ->
          opt.ClientId <- clientId
          opt.ClientSecret <- clientSecret
          opt.CallbackPath <- PathString(callbackPath)
          opt.AuthorizationEndpoint <-  sprintf "https://login.microsoftonline.com/%s/oauth2/v2.0/authorize" tenantId
          opt.TokenEndpoint <- sprintf "https://login.microsoftonline.com/%s/oauth2/v2.0/token" tenantId
          opt.UserInformationEndpoint <- "https://graph.microsoft.com/oidc/userinfo"
          jsonToClaimMap |> Seq.iter (fun (k,v) -> opt.ClaimActions.MapJsonKey(v,k) )
          scopes |> Seq.iter (opt.Scope.Add)
          let ev = opt.Events

          ev.OnCreatingTicket <- Func<_,_> Saturn.Application.parseAndValidateOauthTicket

         ) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = true
      }

    ///Enables AzureAD OAuth authentication with custom configuration
    [<CustomOperation("use_azuread_oauth_with_config")>]
    member __.UseAzureADAuthWithConfig(state: ApplicationState, (config : Authentication.OAuth.OAuthOptions -> unit) ) =
      let middleware (app : IApplicationBuilder) =
        app.UseAuthentication()

      let service (s : IServiceCollection) =
        let c = s.AddAuthentication(fun cfg ->
          cfg.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
          cfg.DefaultChallengeScheme <- "AzureAD")
        addCookie state c
        c.AddOAuth("AzureAD",config) |> ignore
        s

      { state with
          ServicesConfig = service::state.ServicesConfig
          AppConfigs = middleware::state.AppConfigs
          CookiesAlreadyAdded = true
      }
