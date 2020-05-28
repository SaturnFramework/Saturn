module ControllerTests

open Expecto
open Saturn
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http
open System

//---------------------------Routing tests----------------------------------------

let createAction id ctx =
  match id with
  | Some id -> sprintf "Create %i" id
  | None -> "Create"
  |> Controller.text ctx

let updateAction id ctx subId =
    match id with
    | Some id -> sprintf "Update %i %i" id subId
    | None -> sprintf "Update %i" subId
    |> Controller.text ctx

let showAction ctx id =
  sprintf "Create %i" id
  |> Controller.text ctx

let editAction ctx id =
  sprintf "Edit %i" id
  |> Controller.text ctx

let addAction ctx =
  "Add"
  |> Controller.text ctx


let testSubController (id: int) = controller {
  create (createAction (Some id))
  update (updateAction (Some id))
}

let testController = controller {
  subController "/sub" testSubController

  create (createAction None)
  update (updateAction None)

  show showAction
  edit editAction
  add addAction
}

let testPathSegmentController = controller {
  update (fun ctx (id: string) -> sprintf "Update %s" id |> Controller.text ctx)
}

[<Tests>]
let routingTests =
    let responseTestCaseDefault = responseTestCase testController

    testList "Controller Routing Tests" [
        testCase "subController Update works" <|
          responseTestCaseDefault "PUT" "/1/sub/2" "Update 1 2"

        testCase "subController Create trailing slash works" <|
          responseTestCaseDefault "POST" "/1/sub/" "Create 1"

        testCase "subController Create no trailing slash works" <|
          responseTestCaseDefault "POST" "/1/sub" "Create 1"

        testCase "Create trailing slash works" <|
          responseTestCaseDefault "POST" "/" "Create"

        testCase "Create no trailing slash works" <|
          responseTestCaseDefault "POST" "" "Create"

        testCase "Update POST works" <|
          responseTestCaseDefault "POST" "/1" "Update 1"

        testCase "Update PUT works" <|
          responseTestCaseDefault "PUT" "/1" "Update 1"

        testCase "Add works" <|
          responseTestCaseDefault "GET" "/add" "Add"

        testCase "Show works" <|
          responseTestCaseDefault "GET" "/1" "Create 1"

        testCase "Edit worsk" <|
          responseTestCaseDefault "GET" "/1/edit" "Edit 1"


        testCase "deleteAll works" <| fun _ ->
            let expectedStatusCode = 204
            let expectedString = "deleted"
            let mutable plugged = ""
            let deleteAll = fun (ctx: HttpContext) ->
                task {
                    ctx.Response.StatusCode <- 204
                    return (Some ctx)
                }
            let deleteController = controller {
                delete_all deleteAll
                plug [DeleteAll] (fun next ctx -> plugged <- "deleted"; next ctx)
            }
            let deleteResponse =
                getEmptyContext "DELETE" ""
                |> deleteController next
                |> runTask
            match deleteResponse with
            | None -> failtestf "Resulted was expected to be %d, but was %A" expectedStatusCode deleteResponse
            | Some ctx ->
                Expect.equal (ctx.Response.StatusCode) expectedStatusCode "Status code should be 204"
            Expect.equal plugged expectedString "Plugged should equal deleted"

        testCase "deleteAll with trailing slash works" <| fun _ ->
            let expectedStatusCode = 204
            let expectedString = "deleted"
            let mutable plugged = ""
            let deleteAll = fun (ctx: HttpContext) ->
                task {
                    ctx.Response.StatusCode <- 204
                    return (Some ctx)
                }
            let deleteController = controller {
                delete_all deleteAll
                plug [DeleteAll] (fun next ctx -> plugged <- "deleted"; next ctx)
            }
            let deleteResponse =
                getEmptyContext "DELETE" "/"
                |> deleteController next
                |> runTask
            match deleteResponse with
            | None -> failtestf "Resulted was expected to be %d, but was %A" expectedStatusCode deleteResponse
            | Some ctx ->
                Expect.equal (ctx.Response.StatusCode) expectedStatusCode "Status code should be 204"
            Expect.equal plugged expectedString "Plugged should equal deleted"

        testCase "Request `/test/1` returns `test` as controller's key handlers should match up to one path segment" <|
          responseTestCase testPathSegmentController "PUT" "/test" "Update test"

]

//---------------------------Plug tests----------------------------------------


[<Tests>]
let plugTests =
    testList "Controller plug tests" [
        testCase "plugs should only fire once" <| fun _ ->
            let deleteAll = fun (ctx: HttpContext) ->
                task {
                    ctx.Response.StatusCode <- 204
                    return (Some ctx)
                }
            let mutable count = 0
            let controllerWithPlugs =
                controller {
                    create (createAction None)
                    update (updateAction None)
                    delete_all deleteAll
                    plug [All] (fun next ctx -> count <- count + 1; next ctx)
                }
            try
                let postEmpty = getEmptyContext "POST" "" |> controllerWithPlugs next |> runTask
                Expect.equal count 1 "Count should be 1"
                expectResponse "Create" postEmpty
                getEmptyContext "POST" "/" |> controllerWithPlugs next |> runTask |> ignore
                Expect.equal count 2 "Count should be 2"
                getEmptyContext "POST" "/1" |> controllerWithPlugs next |> runTask |> ignore
                Expect.equal count 3 "Count should be 3"
                let putResult = getEmptyContext "PUT" "/1" |> controllerWithPlugs next |> runTask
                expectResponse "Update 1" putResult
                Expect.equal count 4 "Count should be 4"
                let deleteAllResult = getEmptyContext "DELETE" "" |> controllerWithPlugs next |> runTask
                match deleteAllResult with
                | None -> failtestf "deleteAllResult was expected to have status code 204, but was %A" deleteAllResult
                | Some ctx ->
                    Expect.equal (ctx.Response.StatusCode) 204 "status code should be 204"
                Expect.equal count 5 "Count should be 5"

            with ex -> failtestf "failed because %A" ex
    ]

//---------------------------Rendering tests----------------------------------------

//TODO
// let basicTemplate =
//   html [] [
//       head [] []
//       body [] [
//           h1 [] [ encodedText "Hello, world!" ]
//       ]
//   ]

// let implicitNodeToHtmlTestController = controller {
//     index (fun _ -> task { return basicTemplate })
// }

// let explicitNodeToHtmlTestController = controller {
//     index (fun ctx -> task { return! Controller.renderHtml ctx basicTemplate })
// }

// let implicitStringToHtmlTestController = controller {
//    index (fun _ -> task { return renderHtmlNode basicTemplate})
// }

// [<Tests>]
// let htmlRendererTests =
//     testList "Controller HTML rendering" [
//         testCase "doctype is added to implicit index html" <| fun _ ->
//             let ctx = getEmptyContext "GET" "/"
//             ctx.Request.Headers.Add("Accept", StringValues("text/html"))

//             let expectedContent = "<!doctype html>"
//             try
//                 let result = implicitNodeToHtmlTestController next ctx |> runTask
//                 match result with
//                 | None -> failtestf "Calling the endpoint did not yield any result"
//                 | Some ctx ->
//                     let body = getBody ctx
//                     Expect.stringStarts (body.ToUpperInvariant()) (expectedContent.ToUpperInvariant()) "Should start with a doctype element"
//             with ex -> failtestf "failed because %A" ex

//         testCase "doctype is added to explicit index html" <| fun _ ->
//             let ctx = getEmptyContext "GET" "/"
//             ctx.Request.Headers.Add("Accept", StringValues("text/html"))

//             let expectedContent = "<!doctype html>"
//             try
//                 let result = explicitNodeToHtmlTestController next ctx |> runTask
//                 match result with
//                 | None -> failtestf "Calling the endpoint did not yield any result"
//                 | Some ctx ->
//                     let body = getBody ctx
//                     Expect.stringStarts (body.ToUpperInvariant()) (expectedContent.ToUpperInvariant()) "Should start with a doctype element"
//             with ex -> failtestf "failed because %A" ex

//         testCase "doctype is not added to implicit string results" <| fun _ ->
//             let ctx = getEmptyContext "GET" "/"

//             let notExpectedContent = "<!doctype html>"
//             try
//                 let result = implicitStringToHtmlTestController next ctx |> runTask
//                 match result with
//                 | None -> failtestf "Calling the endpoint did not yield any result"
//                 | Some ctx ->
//                     let body = getBody ctx
//                     if body.Contains(notExpectedContent, StringComparison.InvariantCultureIgnoreCase) then
//                         Tests.failtest "Doctype element was present even though it should not be automatically added to string results."
//                     else
//                         ()
//             with ex -> failtestf "failed because %A" ex
//     ]

//---------------------------Implicit conversion tests----------------------------------------
type Test = {A: string; B: int; C: bool}


let implicitConversionsController = controller {
    index (fun _ ->
        task {
            return [{A = "test"; B = 1; C = false}; {A = "test2"; B = 2; C = true}]
        })

    show (fun _ (id : string) ->
        task {
            return {A = "test"; B = 1; C = false}
        })
}

[<Tests>]
let implicitConversionTest =
    let responseTestCase = responseTestCase implicitConversionsController
    testList "Controller implicit conversion" [
        testCase "convert list to JSON" <|
            responseTestCase "GET" "/" """[{"a":"test","b":1,"c":false},{"a":"test2","b":2,"c":true}]"""

        testCase "convert record to JSON" <|
            responseTestCase "GET" "/1" """{"a":"test","b":1,"c":false}"""



    ]

//---------------------------DI tests----------------------------------------

type Dependency = {dep : IDependency}

let diController = controller {
    index (fun ctx d ->
        d.dep.Call ()
        d.dep.Call ()
        let v = d.dep.Value().ToString()
        Controller.text ctx v
    )

    show (fun ctx (d: IDependency) (id: int) ->
        d.Call ()
        d.Call ()
        let v = (id + d.Value()).ToString()
        Controller.text ctx v
    )

    add (fun ctx (d: (IDependency * obj))->
        let (d,_) = d
        d.Call ()
        d.Call ()
        let v = d.Value().ToString()
        Controller.text ctx v
    )
}

[<Tests>]
let automaticDiTest =
    let responseTestCase = responseTestCase diController
    testList "Controller automatic DI" [
        testCase "can inject record" <|
            responseTestCase "GET" "/" "2"

        testCase "can inject interface" <|
            responseTestCase "GET" "/1" "3"

        testCase "can inject tupple" <|
            responseTestCase "GET" "/add" "2"
    ]

//---------------------------Routing tests with string id----------------------------------------

let showActionStrng ctx id =
  sprintf "Create %s" id
  |> Controller.text ctx

let editString ctx id =
  sprintf "Edit %s" id
  |> Controller.text ctx

let addString ctx =
  "Add"
  |> Controller.text ctx


let testStringController = controller {
  show showActionStrng
  edit editString
  add addString
}


[<Tests>]
let routingStringTests =
    let responseTestCaseDefault = responseTestCase testStringController

    testList "Controller Routing with String Tests" [
        testCase "controller add works" <|
          responseTestCaseDefault "GET" "/add" "Add"

        testCase "controller show works" <|
          responseTestCaseDefault "GET" "/abcde" "Create abcde"

        testCase "controller edit worsk" <|
          responseTestCaseDefault "GET" "/abcde/edit" "Edit abcde"


]
