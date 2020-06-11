module BenchmarkTests

open Expecto
open Saturn
open Giraffe
open BenchmarkDotNet
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Configs


let testRouter = router {
  get "/test" (text "HelloWorld")
  getf "/test/%s" (fun s -> text "Test")
  post "/post" (text "post")

  not_found_handler (text "404")
}

let testHandler : HttpHandler = choose [
  GET >=> route "/test" >=> text "HelloWorld"
  GET >=> routef "/test/%s" (fun s -> text "Test")
  POST >=> route "/post" >=> text "post"

  text "404"
]

[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory); CategoriesColumn>]
type Requesters() =

  [<Benchmark; BenchmarkCategory("GET")>]
  member __.SaturnGet() =
    let ctx = getEmptyContext "GET" "/test"
    let _ = testRouter next ctx |> runTask
    ()

  [<Benchmark(Baseline = true); BenchmarkCategory("GET");>]
  member __.GiraffeGet() =
    let ctx = getEmptyContext "GET" "/test"
    let _ = testHandler next ctx |> runTask
    ()

  [<Benchmark; BenchmarkCategory("GETF")>]
  member __.SaturnGetf() =
    let ctx = getEmptyContext "GET" "/test/1"
    let _ = testRouter next ctx |> runTask
    ()

  [<Benchmark(Baseline = true); BenchmarkCategory("GETF");>]
  member __.GiraffeGetf() =
    let ctx = getEmptyContext "GET" "/test/1"
    let _ = testHandler next ctx |> runTask
    ()

  [<Benchmark; BenchmarkCategory("POST")>]
  member __.SaturnPost() =
    let ctx = getEmptyContext "POST" "/post"
    let _ = testRouter next ctx |> runTask
    ()

  [<Benchmark(Baseline = true); BenchmarkCategory("POST");>]
  member __.GiraffePost() =
    let ctx = getEmptyContext "POST" "/post"
    let _ = testHandler next ctx |> runTask
    ()



[<Tests>]
let tests =
  testList "performance tests" [
    test "compare plain Giraffe with Router" {
      let res = BenchmarkRunner.Run<Requesters>()
      ()
    }
  ]
