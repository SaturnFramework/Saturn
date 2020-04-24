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
    type Saturn.Application.ApplicationBuilder with
        /// Enables OpenId authentication with custom configuration
        [<CustomOperation("use_open_id_auth_with_config")>]
        [<Obsolete("This operation has been moved to the Saturn.Extensions.Authorization project.")>]
        member this.UseOpenIdAuthWithConfig_Obsolete(state: ApplicationState, (config: Action<OpenIdConnect.OpenIdConnectOptions>)) =
            this.UseOpenIdAuthWithConfig(state, config)