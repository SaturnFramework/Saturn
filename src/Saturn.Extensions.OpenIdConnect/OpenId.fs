module Saturn

open Saturn

open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection

open System

module OpenId =
    let private addCookie state (c:AuthenticationBuilder) =
        if not state.CookiesAlreadyAdded then
            c.AddCookie() |> ignore

    // Extend Saturn's application { ... } computation expression
    type ApplicationBuilder with
        /// Enables OpenId authentication with custom configuration
        [<CustomOperation("use_open_id_auth_with_config")>]
        member __.UseOpenIdAuthWithConfig(state: ApplicationState, (config: Action<OpenIdConnect.OpenIdConnectOptions>)) =
            let middleware (app : IApplicationBuilder) =
                app.UseAuthentication()

            let service (s: IServiceCollection) =
                let authBuilder = s.AddAuthentication(fun authConfig ->
                    authConfig.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                    authConfig.DefaultChallengeScheme <- OpenIdConnectDefaults.AuthenticationScheme
                    authConfig.DefaultSignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme)
                addCookie state authBuilder
                authBuilder.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, config) |> ignore

                s

            { state with
                ServicesConfig = service::state.ServicesConfig
                AppConfigs = middleware::state.AppConfigs
                CookiesAlreadyAdded = true }
