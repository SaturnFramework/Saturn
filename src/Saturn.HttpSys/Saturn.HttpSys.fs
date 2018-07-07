module Saturn

open Saturn
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Server.HttpSys

type ApplicationBuilder with

    [<CustomOperation("use_httpsys")>]
    member __.UseHttpSys(state) =
        let host (builder: IWebHostBuilder) =
            builder.UseHttpSys ()
        { state with
            HostConfigs = host::state.HostConfigs
        }

    [<CustomOperation("use_httpsys_with_config")>]
    member __.UseHttpSysWithConfig(state, config) =
        let host (builder: IWebHostBuilder) =
            builder.UseHttpSys config
        { state with
            HostConfigs = host::state.HostConfigs
        }

    [<CustomOperation("use_httpsys_windows_auth")>]
    member __.UserHttpSysWindowsAuth(state, allowAnonymous: bool) =
        let host (builder: IWebHostBuilder) =
            builder.UseHttpSys (fun c ->
                c.Authentication.AllowAnonymous <- allowAnonymous
                c.Authentication.Schemes <- AuthenticationSchemes.Negotiate ||| AuthenticationSchemes.NTLM)
        let service (builder: IServiceCollection) =
            builder.AddAuthentication HttpSysDefaults.AuthenticationScheme |> ignore
            builder
        { state with
            HostConfigs = host::state.HostConfigs
            ServicesConfig = service::state.ServicesConfig
        }

