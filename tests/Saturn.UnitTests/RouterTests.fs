module SampleTests

open Expecto
open Saturn
open Giraffe

let testRouter = router {
  get "/1" (text "HelloWorld")
  get "/post/1" (text "Foo")
  getf "/user/%i" (fun (userId: int32) -> (sprintf "User Id: %i" userId) |> text)
  getf "/isbn/%d" (fun (bookRef: int64) -> (sprintf "Book ISBN: %d" bookRef) |> text)
  post "/post/1" (text "1")
  post "/post/2" (text "2")
  put "/user/123" (text "User updated!")
  delete "/user/123" (text "User deleted!")
  deletef "/user/%f" (fun (userId : float) -> (sprintf "The user deleted is: %0.2f" userId) |> text)
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

    testCase "GET to `/user/18` returns `User Id: 18`" <| fun _ ->
      let ctx = getEmptyContext "GET" "/user/18"

      let expected = "User Id: 18"
      let result = testRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "GET to `/isbn/9781491951170` returns `Book ISNB: 9781491951170`" <| fun _ ->
      let ctx = getEmptyContext "GET" "/isbn/9781491951170"
  
      let expected = "Book ISBN: 9781491951170"
      let result = testRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "PUT to `/user/123` returns `User updated!`" <| fun _ ->
      let ctx = getEmptyContext "PUT" "/user/123"
  
      let expected = "User updated!"
      let result = testRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "DELETE to `/user/123` returns `User deleted!`" <| fun _ ->
      let ctx = getEmptyContext "DELETE" "/user/123"
  
      let expected = "User deleted!"
      let result = testRouter next ctx |> runTask
      match result with
      | None -> failtestf "Result was expected to be %s" expected
      | Some ctx ->
        Expect.equal (getBody ctx) expected "Result should be equal"

    testCase "DELETEF to `/user/1.0` returns `The user deleted is: 1.0`" <| fun _ ->
      let ctx = getEmptyContext "DELETE" "/user/1.0"
  
      let expected = "The user deleted is: 1.0"
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
