module SimpleTests

open Expecto
open Saturn
open Giraffe

//---------------------------`Response.accepted` tests----------------------------------------

type Link = { id: string; links: string list}

let somePost : HttpHandler =
  fun _ ctx ->
    task {
      let id = "myId"
      let mkLinks () = [
        "myLink1"
        "myLink2"
      ]

      return!
        { id = id
          links = mkLinks () }
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

//---------------------------`Application only takes one router` tests----------------------------------------

[<Tests>]
let routerTests =
  testList "Application only takes one router" [
    testCase "Second router throws" (fun _ ->
      let app () =
        application {
          use_router (text "")
          use_router (text "")
        }

      Expect.throws (app >> ignore) "Application did not fail on second router!"
    )
    testCase "Adding a router after `no_router` throws" (fun _ ->
      let app () =
        application {
          no_router
          use_router (text "")
        }

      Expect.throws (app >> ignore) "Application did not fail on router after no_router!"
    )
    testCase "Adding a `no_router after `use_router` throws" (fun _ ->
      let app () =
        application {
          use_router (text "")
          no_router
        }

      Expect.throws (app >> ignore) "Application did not fail on no_router after use_router!"
    )
  ]
