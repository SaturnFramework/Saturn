module SampleTests

open Expecto
open Saturn
open Giraffe

let testRouter = router {
  get "/1" (text "HelloWorld")
  get "/post/1" (text "Foo")
  post "/post/1" (text "1")
  post "/post/2" (text "2")
}

[<Tests>]
let tests =
  testList "Basic tests" [
    testCase "POST to `/post/1` returns `1`" <| fun _ ->
      let ctx = getEmptyContext "POST" "/post/1"

      let expected = "1"
      let result = testRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "POST to `/post/2` returns `2`" <| fun _ ->
      let ctx = getEmptyContext "POST" "/post/2"

      let expected = "2"
      let result = testRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "GET to `/post/1` returns `Foo`" <| fun _ ->
      let ctx = getEmptyContext "GET" "/post/1"

      let expected = "Foo"
      let result = testRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "GET to `1` returns `HelloWorld`" <| fun _ ->
      let ctx = getEmptyContext "GET" "/1"

      let expected = "HelloWorld"
      let result = testRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"


  ]

let testCiRouter = router {
  case_insensitive

  get "/1" (text "HelloWorld")
  get "/post/1" (text "Foo")
  post "/post/1" (text "1")
  post "/post/2" (text "2")
}

[<Tests>]
let caseInsensitiveTest =
  testList "Case Insensitve tests" [
    testCase "POST to `/POST/1` returns `1`" <| fun _ ->
      let ctx = getEmptyContext "POST" "/POST/1"

      let expected = "1"
      let result = testCiRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "POST to `/POST/2` returns `2`" <| fun _ ->
      let ctx = getEmptyContext "POST" "/POST/2"

      let expected = "2"
      let result = testCiRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "GET to `/POST/1` returns `Foo`" <| fun _ ->
      let ctx = getEmptyContext "GET" "/POST/1"

      let expected = "Foo"
      let result = testCiRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "GET to `1` returns `HelloWorld`" <| fun _ ->
      let ctx = getEmptyContext "GET" "/1"

      let expected = "HelloWorld"
      let result = testCiRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"


  ]

let rootId = System.Guid.NewGuid()
let root = "roots"

let testForwardingRouter =
    router {
        forward $"/{root}" (fun next context ->
            text $"{root}" next context
            )
        forwardf "/roots/%O" (fun id next context -> 
            text (sprintf "roots/%O" id) next context
            )
    }

[<Tests>]
let forwardingTests =
    testList "Common root forwarding tests" [
        testCase "FORWARD to `/ROOTS` returns `roots`" <| fun _ ->
            let ctx = getEmptyContext "FORWARD" $"/{root}"
            let expected = root
            let result = testForwardingRouter next ctx |> runTask
            match result with
            | None -> failtestf "Result was expected to be %s" expected
            | Some ctx ->
                let body = (getBody ctx)
                Expect.equal body expected "Result should be equal"

        testCase $"FORWARD to `/ROOTS/{rootId}` returns `roots/{rootId}`" <| fun _ ->
            let ctx = getEmptyContext "FORWARD" $"/{root}/{rootId}"
            let expected = $"{root}/{rootId}"
            let result = testForwardingRouter next ctx |> runTask
            match result with
            | None -> failtestf "Result was expected to be %s" expected
            | Some ctx ->
                let body = (getBody ctx)
                Expect.equal body expected "Result should be equal"
    ]
