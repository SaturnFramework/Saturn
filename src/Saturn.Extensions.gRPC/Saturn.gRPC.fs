module Saturn

open Saturn
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open ProtoBuf.Grpc.Server

type Saturn.Application.ApplicationBuilder with

    [<CustomOperation("use_grpc")>]
    ///Adds gRPC Code First endpoint. Passed parameter should be any constructor of the gRPC service implementation.
    member __.UseGrpc<'a, 'b when 'a : not struct>(state, cons: 'b -> 'a) =
        let configureServices (services: IServiceCollection) =
            services.AddCodeFirstGrpc()
            services

        let configureApp (app: IApplicationBuilder) =
            app.UseRouting()

        let configureGrpcEndpoint (app: IApplicationBuilder) =
            app.UseEndpoints (fun endpoints -> endpoints.MapGrpcService<'a>() |> ignore)

        { state with
            AppConfigs = configureApp::configureGrpcEndpoint::state.AppConfigs.[1.. state.AppConfigs.Length]
            ServicesConfig = configureServices::state.ServicesConfig
        }
