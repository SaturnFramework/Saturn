module Sample

open Saturn
open Microsoft.AspNetCore.Server.Kestrel.Core

open Shared
open System.Threading.Tasks

type MyCalculator() =
    interface ICalculator with
        member __.MultiplyAsync request =
            ValueTask<_> { Result = request.X * request.Y }

let app = application {
  no_router
  listen_local 10042 (fun opts -> opts.Protocols <- HttpProtocols.Http2)
  use_grpc MyCalculator
}


[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code
