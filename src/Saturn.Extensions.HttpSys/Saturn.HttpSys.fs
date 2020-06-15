module Saturn

open Saturn
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Server.HttpSys

type Saturn.Application.ApplicationBuilder with

    /// HTTP.sys is a web server for ASP.NET Core that only runs on Windows. HTTP.sys is an alternative to Kestrel server and offers some features that Kestrel doesn't provide.
    /// (https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys)
    /// This operation switches hosting to the HTTP.sys server.
    [<CustomOperation("use_httpsys")>]
    member __.UseHttpSys(state) =
        let host (builder: IWebHostBuilder) =
            builder.UseHttpSys ()
        { state with
            WebHostConfigs = host::state.WebHostConfigs
        }

    /// HTTP.sys is a web server for ASP.NET Core that only runs on Windows. HTTP.sys is an alternative to Kestrel server and offers some features that Kestrel doesn't provide.
    /// (https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys)
    /// This operation switches hosting to the HTTP.sys server and takes additional config.
    [<CustomOperation("use_httpsys_with_config")>]
    member __.UseHttpSysWithConfig(state, config) =
        let host (builder: IWebHostBuilder) =
            builder.UseHttpSys config
        { state with
            WebHostConfigs = host::state.WebHostConfigs
        }

    /// HTTP.sys is a web server for ASP.NET Core that only runs on Windows. HTTP.sys is an alternative to Kestrel server and offers some features that Kestrel doesn't provide.
    /// (https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys)
    /// This operation switches hosting to the HTTP.sys server and enables Windows Auth (NTLM/Negotiate).
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
            WebHostConfigs = host::state.WebHostConfigs
            ServicesConfig = service::state.ServicesConfig
        }

