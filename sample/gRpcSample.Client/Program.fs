﻿open ProtoBuf.Grpc.Client
open Shared
open System
open System.Net.Http

[<EntryPoint>]
let main _ =
    HttpClientExtensions.AllowUnencryptedHttp2 <- true
    task {
        use http = new HttpClient (BaseAddress = Uri "http://localhost:10042")
        let client = http.CreateGrpcService<ICalculator>()
        let! result = client.MultiplyAsync { X = 12; Y = 4 }
        printfn "%i" result.Result
        return 0
    } |> fun t -> t.Result
