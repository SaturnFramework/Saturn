module Controller.Sample

open Saturn
open Giraffe.Core
open Giraffe.ResponseWriters
open Giraffe
open System
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Hosting

type SampleDeps = {logger : ILogger<obj>}

let commentController userId = controller {
    index (fun ctx deps ->
        deps.logger.LogInformation("Index Action")
        (sprintf "Comment Index handler for user %i" userId ) |> Controller.text ctx)
    add (fun ctx deps ->
        deps.logger.LogInformation("Add Action")
        (sprintf "Comment Add handler for user %i" userId ) |> Controller.text ctx)
    show (fun ctx id deps ->
        deps.logger.LogInformation("Show Action")
        (sprintf "Show comment %s handler for user %i" id userId ) |> Controller.text ctx)
    edit (fun ctx id deps ->
        deps.logger.LogInformation("Edit Action")
        (sprintf "Edit comment %s handler for user %i" id userId )  |> Controller.text ctx)
}

type OtherSampleDeps = ILogger<obj> * IHostingEnvironment

let userControllerVersion1 = controller {
    version "1"
    subController "/comments" commentController

    index (fun ctx (deps : OtherSampleDeps) ->
        let log, env = deps
        log.LogInformation("Index Action")
        "Index handler version 1" |> Controller.text ctx)
    add (fun ctx (deps : OtherSampleDeps)->
        let log, env = deps
        log.LogInformation("Add Action")
        "Add handler version 1" |> Controller.text ctx)
    show (fun ctx id _ -> (sprintf "Show handler version 1 - %i" id) |> Controller.text ctx)
    edit (fun ctx id _ -> (sprintf "Edit handler version 1 - %i" id) |> Controller.text ctx)
}

let userController = controller {
    subController "/comments" commentController

    plug [All] (setHttpHeader "user-controller-common" "123")
    plug [Index; Show] (setHttpHeader "user-controller-specialized" "123")

    index (fun ctx (logger: ILogger<obj>)->
        logger.LogInformation("Index Action")
        "Index handler no version" |> Controller.text ctx)
    show (fun ctx id (logger: ILogger<obj>) -> (sprintf "Show handler no version - %i" id) |> Controller.text ctx)
    add (fun ctx _ -> "Add handler no version" |> Controller.text ctx)
    create (fun ctx _ -> "Create handler no version" |> Controller.text ctx)
    edit (fun ctx id _ -> (sprintf "Edit handler no version - %i" id) |> Controller.text ctx)
    update (fun ctx id _ -> (sprintf "Update handler no version - %i" id) |> Controller.text ctx)
    delete (fun ctx id _ -> failwith (sprintf "Delete handler no version failed - %i" id) |> Controller.text ctx)
    error_handler (fun ctx ex -> sprintf "Error handler no version - %s" ex.Message |> Controller.text ctx)
}

type Response = {
    a: string
    b: string
}

type DifferentResponse = {
    c: int
    d: DateTime
}

let typedController = controller {
    index (fun _ _-> task {
        return {a = "hello"; b = "world"}
    })

    add (fun _ _ -> task {
        return {c = 123; d = DateTime.Now}
    })
}

let otherRouter = router {
    get "/dsa" (text "")
    getf "/dsa/%s" (text)
    forwardf "/ddd/%s" (fun (_ : string) -> userControllerVersion1)
    not_found_handler (setStatusCode 404 >=> text "Not Found")
}

let topRouter = router {
    forward "/users" userControllerVersion1
    forward "/users" userController
    forward "/typed" typedController
    forwardf "/%s/%s/abc" (fun (_ : string * string) -> otherRouter)
}

let app = application {
    use_router topRouter
    url "http://0.0.0.0:8085/"
}

[<EntryPoint>]
let main _ =
    run app
    0
