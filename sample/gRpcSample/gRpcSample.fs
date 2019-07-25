module Sample

open Saturn
open Microsoft.AspNetCore.Server.Kestrel.Core

open Shared
open System.Threading.Tasks

open Giraffe.Serialization

type MyCalculator() =
    interface ICalculator with
        member __.MultiplyAsync request =
            ValueTask<_> { Result = request.X * request.Y }

type MyCalculatorWithDI(serializer: IJsonSerializer) =
    interface ICalculator with
        member __.MultiplyAsync request =
            printfn "Multiply reques serialized: %s" (serializer.SerializeToString request)
            ValueTask<_> { Result = request.X * request.Y }

let app = application {
  no_router
  listen_local 10042 (fun opts -> opts.Protocols <- HttpProtocols.Http2)
  use_grpc MyCalculatorWithDI
}


[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code
