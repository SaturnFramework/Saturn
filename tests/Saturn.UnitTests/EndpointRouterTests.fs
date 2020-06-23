module EndpointRouterTests

open Expecto
open Giraffe
open Saturn.Endpoint

let testRouter = router {
  get "/1" (text "HelloWorld")
  get "/post/1" (text "Foo")
  post "/post/1" (text "1")
  post "/post/2" (text "2")
}

[<Tests>]
let caseInsensitiveTest =
  testList "Endpoint router tests" [
    let responseTestCase () = responseEndpointTestCase (hostFromController testRouter)
    testCase "POST to `/POST/1` returns `1`" <|
      responseTestCase () "POST" "/POST/1" "1"


    testCase "POST to `/POST/2` returns `2`" <|
      responseTestCase () "POST" "/POST/2" "2"

    testCase "GET to `/POST/1` returns `Foo`" <|
      responseTestCase () "GET" "/POST/1" "Foo"

    testCase "GET to `/1` returns `HelloWorld`" <|
      responseTestCase () "GET" "/1" "HelloWorld"
  ]
