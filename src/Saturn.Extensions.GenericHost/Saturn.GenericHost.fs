namespace Saturn
open Microsoft.Extensions.Hosting
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Threading
open Microsoft.Extensions.DependencyInjection

module GenericHost =
    type Worker() =
        inherit BackgroundService()
        override __.ExecuteAsync(ct: CancellationToken) =
            task {
                while not ct.IsCancellationRequested do
                do! Tasks.Task.Delay(1000, ct)
            } :> Tasks.Task

    type HostState = {
        ServiceConfigs: (IServiceCollection -> IServiceCollection) list
        HostConfigs: (IHostBuilder -> IHostBuilder) list
        CliArguments: string array option
    }

    type HostBuilderCE internal () =
        member __.Yield(_) =
            { HostConfigs = []; ServiceConfigs = []; CliArguments = None }
        member __.Run(state: HostState) : IHostBuilder =
            let defaultHost = Host.CreateDefaultBuilder()

            defaultHost