module ControllerTests

open Expecto
open Saturn
open Giraffe
open Giraffe.GiraffeViewEngine
open Microsoft.Extensions.Primitives
open Microsoft.AspNetCore.Http
open System

let count = System.Collections.Generic.Dictionary<string, int>()

let createAction =
    fun ctx -> "Create" |> Controller.text ctx

let updateAction =
    fun ctx id -> (sprintf "Update %i" id) |> Controller.text ctx

let updateCount : HttpHandler =
    fun next ctx ->
        let method = ctx.Request.Method
        if count.ContainsKey method then
            count.[method] <- count.[method] + 1
        else
            count.Add(method, 1)
        next ctx

let testController = controller {
    create createAction
    update updateAction
    plug [Create;Update] updateCount

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
            let ctx = getEmptyContext "POST" "/"

            let expected = "Create"
            try
                try
                    let result = testController next ctx |> runTask
                    match result with
                    | None -> failtestf "Result was expected to be %s, but was %A" expected result
                    | Some ctx ->
                        Expect.equal (getBody ctx) expected "Result should be equal"

                        Expect.equal (count.["POST"]) 1 "Count should be 1"
                with ex -> failtestf "failed because %A" ex
            finally
                ()

        testCase "update works" <|  fun _ ->
            let ctx = getEmptyContext "PUT" "/1"

            let expected = "Update 1"
            try
                try
                    let result = testController next ctx |> runTask
                    match result with
                    | None -> failtestf "Result was expected to be %s, but was %A" expected result
                    | Some ctx ->
                        Expect.equal (getBody ctx) expected "Result should be equal"
                        Expect.equal (count.["PUT"]) 1 "Count should be 1"
                with ex -> failtestf "failed because %A" ex
            finally
                ()

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
