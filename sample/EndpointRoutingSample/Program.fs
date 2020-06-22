module Sample

open Saturn
open Saturn.Endpoint //Use Endpoint router
open Giraffe

//Saturn is using standard HttpHandlers from Giraffe

let apiHelloWorld = text "hello world from API"
let apiHelloWorld2 = text "hello world from API 2"
let apiDeleteExample = text "this is a delete example"
let apiDeleteExample2 str = sprintf "Echo: %s" str |> text
let otherHelloWorld = text "hello world from OTHER"
let otherHelloWorld2 = text "hello world from OTHER 2"
let helloWorld = text "hello world"
let helloWorld2 = text "hello world2"
let helloWorldName str = text ("hello world, " + str)
let helloWorldNameAge (str, age) = text (sprintf "hello world, %s, You're %i" str age)

let apiHeaderPipe = pipeline {
    set_header "myCustomHeaderApi" "api"
}

let otherHeaderPipe = pipeline {
    set_header "myCustomHeaderOther" "other"
}

let headerPipe = pipeline {
    set_header "myCustomHeader" "abcd"
    set_header "myCustomHeader2" "zxcv"
}

let endpointPipe = pipeline {
    plug fetchSession
    plug head
    plug requestId
}


let apiRouter = router {
    get "/" apiHelloWorld
    get "/a" apiHelloWorld2
    delete "/del" apiDeleteExample
    deletef "/del/%s" apiDeleteExample2
}


let commentController key = subcontroller {
    not_found_handler (setStatusCode 404 >=> text "Comment 404")

    index (fun ctx ->  (sprintf "Comment Index handler; Key %s" key) |> Controller.text ctx)
    add (fun ctx -> (sprintf "Comment Add handler; Key %s" key) |> Controller.text ctx)
    show (fun ctx id -> (sprintf "Comment Show handler - %s; Key %s" id key) |> Controller.text ctx)
    edit (fun ctx id -> (sprintf "Comment Edit handler - %s; Key %s" id key) |> Controller.text ctx)
}

let userController = controller {
    not_found_handler (setStatusCode 404 >=> text "Users 404")

    index (fun ctx -> "Index handler" |> Controller.text ctx)
    add (fun ctx -> "Add handler" |> Controller.text ctx)
    show (fun ctx id -> (sprintf "Show handler - %s" id) |> Controller.text ctx)
    edit (fun ctx id -> (sprintf "Edit handler - %s" id) |> Controller.text ctx)
    subController "/comment" commentController
}


let topRouter = router {
    pipe_through headerPipe
    //TODO
    // not_found_handler (SiteMap.page)

    get "/" helloWorld
    get "/a" helloWorld2
    getf "/name/%s" helloWorldName
    getf "/name/%s/%i" helloWorldNameAge

    forward "/other" (router {
        pipe_through otherHeaderPipe

        get "/" otherHelloWorld
        get "/a" otherHelloWorld2
    })

    // or can be defined separatly and used as HttpHandler
    forward "/api" apiRouter

    // same with controllers
    forward "/users" userController
}

let app = application {
    pipe_through endpointPipe

    use_endpoint_router topRouter //Use Endpoint router
    url "http://0.0.0.0:8085/"
    memory_cache
    use_static "static"
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code

