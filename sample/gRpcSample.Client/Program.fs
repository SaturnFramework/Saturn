open ProtoBuf.Grpc.Client
open Shared
open System
open System.Net.Http
open FSharp.Control.Tasks
open Grpc.Net.Client

[<EntryPoint>]
let main _ =
    HttpClientExtensions.AllowUnencryptedHttp2 <- true
    task {
        use http = GrpcChannel.ForAddress("http://localhost:10042");
        let calculatorClient = http.CreateGrpcService<ICalculator>()
        let helloClient = http.CreateGrpcService<IHello>()

        let! result = calculatorClient.MultiplyAsync { X = 12; Y = 4 }
        printfn "%i" result.Result
        
        let! r = helloClient.TestAsync { Parameter = "Saturn" }
        printfn "%s" r.Response
        
        return 0
    } |> fun t -> t.Result