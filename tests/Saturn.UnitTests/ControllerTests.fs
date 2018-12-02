module ControllerTests

open Expecto
open Saturn
open Giraffe
open Giraffe.GiraffeViewEngine
open Microsoft.Extensions.Primitives
open Microsoft.AspNetCore.Http
open System

let createAction =
    fun ctx ->
        "Create" |> Controller.text ctx

let updateAction =
    fun ctx id -> (sprintf "Update %i" id) |> Controller.text ctx

let testController = controller {
    create createAction
    update updateAction
}

let basicTemplate =
    html [] [
        head [] []
        body [] [
            h1 [] [ encodedText "Hello, world!" ]
        ]
    ]

let implicitNodeToHtmlTestController = controller {
    index (fun _ -> task { return basicTemplate })
}

let explicitNodeToHtmlTestController = controller {
    index (fun ctx -> task { return! Controller.renderHtml ctx basicTemplate })
}

let implicitStringToHtmlTestController = controller {
   index (fun _ -> task { return GiraffeViewEngine.renderHtmlNode basicTemplate})
}

[<Tests>]
let tests =
    testList "Controller Tests" [
        testCase "create works" <|  fun _ ->
            let ctx = getEmptyContext "POST" ""
            let expected = "Create"

            try
                let result = testController next ctx |> runTask
                match result with
                | None -> failtestf "Result was expected to be %s, but was %A" expected result
                | Some ctx ->
                    Expect.equal (getBody ctx) expected "Result should be equal"
            with ex -> failtestf "failed because %A" ex

        testCase "update works" <| fun _ ->
            let ctx = getEmptyContext "PUT" "/1"
            let expected = "Update 1"

            try
                let result = testController next ctx |> runTask
                match result with
                | None -> failtestf "Result was expected to be %s, but was %A" expected result
                | Some ctx ->
                    Expect.equal (getBody ctx) expected "Result should be equal"

            with ex -> failtestf "failed because %A" ex

        testCase "deleteAll works" <| fun _ ->
            let expectedStatusCode = 204
            let expectedString = "deleted"
            let mutable plugged = ""
            let deleteAll = fun (ctx: HttpContext) ->
                task {
                    do ctx.SetStatusCode 204
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
                    do ctx.SetStatusCode 204
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

        testCase "plugs should only fire once" <| fun _ ->
            let deleteAll = fun (ctx: HttpContext) ->
                task {
                    do ctx.SetStatusCode 204
                    return (Some ctx)
                }
            let mutable count = 0
            let controllerWithPlugs =
                controller {
                    create createAction
                    update updateAction
                    delete_all deleteAll
                    plug [All] (fun next ctx -> count <- count + 1; next ctx)
                }
            try
                let postEmpty = getEmptyContext "POST" "" |> controllerWithPlugs next |> runTask
                Expect.equal count 1 "Count should be 1"
                match postEmpty with
                | None -> failtestf "Result was expected to be %s, but was %A" "Create" postEmpty
                | Some ctx ->
                    Expect.equal (getBody ctx) "Create" "Result should be equal"
                getEmptyContext "POST" "/" |> controllerWithPlugs next |> runTask |> ignore
                Expect.equal count 2 "Count should be 2"
                getEmptyContext "POST" "/1" |> controllerWithPlugs next |> runTask |> ignore
                Expect.equal count 3 "Count should be 3"
                let putResult = getEmptyContext "PUT" "/1" |> controllerWithPlugs next |> runTask
                match putResult with
                | None -> failtestf "Result was expected to be %s, but was %A" "Create" postEmpty
                | Some ctx ->
                    Expect.equal (getBody ctx) "Update 1" "Result should be equal"
                Expect.equal count 4 "Count should be 4"
                let deleteAllResult = getEmptyContext "DELETE" "" |> controllerWithPlugs next |> runTask
                match deleteAllResult with
                | None -> failtestf "deleteAllResult was expected to have status code 204, but was %A" deleteAllResult
                | Some ctx ->
                    Expect.equal (ctx.Response.StatusCode) 204 "status code should be 204"
                Expect.equal count 5 "Count should be 5"

            with ex -> failtestf "failed because %A" ex

        testCase "doctype is added to implicit index html" <| fun _ ->
            let ctx = getEmptyContext "GET" "/"
            ctx.Request.Headers.Add("Accept", StringValues("text/html"))

            let expectedContent = "<!doctype html>"
            try
                let result = implicitNodeToHtmlTestController next ctx |> runTask
                match result with
                | None -> failtestf "Calling the endpoint did not yield any result"
                | Some ctx ->
                    let body = getBody ctx
                    Expect.stringStarts (body.ToUpperInvariant()) (expectedContent.ToUpperInvariant()) "Should start with a doctype element"
            with ex -> failtestf "failed because %A" ex

        testCase "doctype is added to explicit index html" <| fun _ ->
            let ctx = getEmptyContext "GET" "/"
            ctx.Request.Headers.Add("Accept", StringValues("text/html"))

            let expectedContent = "<!doctype html>"
            try
                let result = explicitNodeToHtmlTestController next ctx |> runTask
                match result with
                | None -> failtestf "Calling the endpoint did not yield any result"
                | Some ctx ->
                    let body = getBody ctx
                    Expect.stringStarts (body.ToUpperInvariant()) (expectedContent.ToUpperInvariant()) "Should start with a doctype element"
            with ex -> failtestf "failed because %A" ex

        testCase "doctype is not added to implicit string results" <| fun _ ->
            let ctx = getEmptyContext "GET" "/"

            let notExpectedContent = "<!doctype html>"
            try
                let result = implicitStringToHtmlTestController next ctx |> runTask
                match result with
                | None -> failtestf "Calling the endpoint did not yield any result"
                | Some ctx ->
                    let body = getBody ctx
                    if body.Contains(notExpectedContent, StringComparison.InvariantCultureIgnoreCase) then
                        Tests.failtest "Doctype element was present even though it should not be automatically added to string results."
                    else
                        ()
            with ex -> failtestf "failed because %A" ex
]
