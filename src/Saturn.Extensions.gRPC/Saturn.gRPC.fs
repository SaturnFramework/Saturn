module Saturn

open Saturn
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open ProtoBuf.Grpc.Server

type ApplicationBuilder with

    [<CustomOperation("use_gprc")>]
    member __.UseGrpc<'a when 'a : not struct>(state) =
        let configureServices (services: IServiceCollection) =
            services.AddCodeFirstGrpc()
            services

        let configureApp (app: IApplicationBuilder) =
            app.UseRouting()

        let configureGrpcEndpoint (app: IApplicationBuilder) =
            app.UseEndpoints (fun endpoints -> endpoints.MapGrpcService<'a>() |> ignore)

        { state with
            AppConfigs = configureApp::configureGrpcEndpoint::state.AppConfigs
            ServicesConfig = configureServices::state.ServicesConfig
        }

