module Controller.Sample

open Saturn.Controller
open Saturn.ControllerHelpers 
open Saturn.Router
open Giraffe.HttpHandlers
open Saturn.Application

let commentController userId = controller {
    index (fun ctx -> (sprintf "Comment Index handler for user %i" userId ) |> Controller.text ctx)
    add (fun ctx -> (sprintf "Comment Add handler for user %i" userId ) |> Controller.text ctx)
    show (fun (ctx, id) -> (sprintf "Show comment %s handler for user %i" id userId ) |> Controller.text ctx)
    edit (fun (ctx, id) -> (sprintf "Edit comment %s handler for user %i" id userId )  |> Controller.text ctx)
}
 
let userControllerVersion1 = controller {
    version 1
    subController "/comments" commentController 

    index (fun ctx -> "Index handler version 1" |> Controller.text ctx)
    add (fun ctx -> "Add handler version 1" |> Controller.text ctx)
    show (fun (ctx, id) -> (sprintf "Show handler version 1 - %i" id) |> Controller.text ctx)
    edit (fun (ctx, id) -> (sprintf "Edit handler version 1 - %i" id) |> Controller.text ctx)
}

let userController = controller {   
    subController "/comments" commentController     

    index (fun ctx -> "Index handler no version" |> Controller.text ctx)
    add (fun ctx -> "Add handler no version" |> Controller.text ctx)
    show (fun (ctx, id) -> (sprintf "Show handler no version - %i" id) |> Controller.text ctx)
    edit (fun (ctx, id) -> (sprintf "Edit handler no version - %i" id) |> Controller.text ctx)
}

let topRouter = scope {
    error_handler (setStatusCode 404 >=> text "Not Found")
    
    forward "/users" userControllerVersion1
    forward "/users" userController
}

let app = application {
    router topRouter
    url "http://0.0.0.0:8085/"
}

[<EntryPoint>]
let main _ =
    run app 
    0
