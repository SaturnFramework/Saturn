module Saturn

open Saturn
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open ProtoBuf.Grpc.Server

type Saturn.Application.ApplicationBuilder with

    [<CustomOperation("use_grpc")>]
    ///Adds gRPC Code First endpoint. Passed parameter should be parameter-less constructor of the gRPC service implementation.
    member __.UseGrpc<'a when 'a : not struct>(state, cons: unit -> 'a) =
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
