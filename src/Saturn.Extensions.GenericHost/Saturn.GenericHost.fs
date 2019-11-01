namespace Saturn
open Microsoft.Extensions.Hosting
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Threading
open Microsoft.Extensions.DependencyInjection

[<AutoOpen>]
module GenericHost =

    type HostState = {
        ServiceConfigs: (IServiceCollection -> IServiceCollection) list
        HostConfigs: (IHostBuilder -> IHostBuilder) list
        CliArguments: string array option
    }

    type HostBuilderCE internal () =
        member __.Yield(_) =
            { HostConfigs = []; ServiceConfigs = []; CliArguments = None }

        member __.Run(state: HostState) : IHostBuilder =
            let defaultHost =
                Host.CreateDefaultBuilder(Option.toObj state.CliArguments)
                |> List.foldBack (fun e acc -> e acc ) state.HostConfigs

            defaultHost.ConfigureServices(fun _ services ->
                 state.ServiceConfigs |> List.rev |> List.iter (fun fn -> fn services |> ignore) |> ignore)

        ///Adds custom host configuration step.
        [<CustomOperation("host_config")>]
        member __.HostConfig(state, config) =
          {state with HostConfigs = config::state.HostConfigs}

        ///Adds custom service configuration step.
        [<CustomOperation("service_config")>]
        member __.ServiceConfig(state, config) =
          {state with ServiceConfigs = config::state.ServiceConfigs}

        ///Sets the cli arguments for the `IWebHostBuilder` to enable default command line configuration and functionality.
        [<CustomOperation("cli_arguments")>]
        member __.CliArguments (state, args) =
           { state with CliArguments = Some args }

    let host = HostBuilderCE()

    let run (hostBuilder:IHostBuilder) = hostBuilder.Build().Run()