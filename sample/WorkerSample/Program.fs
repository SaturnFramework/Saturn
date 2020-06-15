module Sample

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open FSharp.Control.Tasks.V2.ContextInsensitive
open System.Threading
open Microsoft.Extensions.DependencyInjection
open Saturn

type Worker(logger:ILogger<Worker>) =
    inherit BackgroundService()
    override __.ExecuteAsync(ct: CancellationToken) =
            ct.Register(fun () -> logger.LogInformation("Worker canceled at: {time}", System.DateTimeOffset.Now)) |> ignore
            task {
                while not ct.IsCancellationRequested do
                logger.LogInformation("Worker running at: {time}", System.DateTimeOffset.Now)
                do! Tasks.Task.Delay(1000, ct)
            } :> Tasks.Task

[<EntryPoint>]
let main argv =
    let h =
        application {
            no_webhost //Don't start default webhost

            cli_arguments argv
            service_config (fun s -> s.AddHostedService<Worker>())
        }
    run h
    0 // return an integer exit code
