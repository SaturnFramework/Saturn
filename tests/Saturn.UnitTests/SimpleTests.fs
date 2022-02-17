module SimpleTets

open Expecto
open Saturn
open Giraffe

//---------------------------`Response.accepted` tests----------------------------------------

type Link = { id: string; links: string list}

let somePost : HttpHandler =
  fun _ ctx ->
    task {
      let id = "myId"
      let mkLinks x = [
        "myLink1"
        "myLink2"
      ]


      return!
        { id = id
          links = mkLinks id }
        |> Response.accepted ctx
    }

let router = router {
    post "/" somePost
}

[<Tests>]
let tests =
  testList "Response.acepted tests" [
    testCase "Correct status code and body" <| fun _ ->
      let ctx = getEmptyContext "POST" "/"

      let result = router next ctx |> runTask
      match result with
      | None -> failtestf "HttpHandler returned None"
      | Some ctx ->
        Expect.equal (ctx.Response.StatusCode) 202 "Result should be accepted"
        Expect.equal (getBody ctx) """{"id":"myId","links":["myLink1","myLink2"]}""" "Result should be equal"

  ]
