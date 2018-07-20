module ControllerTests

open Expecto
open Saturn
open Giraffe

let createAction =
    fun ctx -> "Create" |> Controller.text ctx

let updateAction =
    fun ctx id -> (sprintf "Update %i" id) |> Controller.text ctx

let testController = controller {
    create createAction
    update updateAction
}

[<Tests>]
let tests =
    testList "Controller Tests" [
        testCase "create works" <|  fun _ ->
            let ctx = getEmptyContext "POST" "/"

            let expected = "Create"
            try
                let result = testController next ctx |> runTask
                match result with
                | None -> failtestf "Result was expected to be %s, but was %A" expected result
                | Some ctx ->
                Expect.equal (getBody ctx) expected "Result should be equal"
            with ex -> failtestf "failed because %A" ex

        testCase "update works" <|  fun _ ->
            let ctx = getEmptyContext "POST" "/1"

            let expected = "Update 1"
            try
                let result = testController next ctx |> runTask
                match result with
                | None -> failtestf "Result was expected to be %s, but was %A" expected result
                | Some ctx ->
                Expect.equal (getBody ctx) expected "Result should be equal"
            with ex -> failtestf "failed because %A" ex
]