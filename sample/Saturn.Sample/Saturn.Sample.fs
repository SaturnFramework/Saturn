module Saturn.Sample

open Saturn.Router

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open Giraffe
open Microsoft.AspNetCore

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

let apiHelloWorld = pipeline {text "hello world from API"}
let apiHelloWorld2 = pipeline {text "hello world from API 2"}
let otherHelloWorld = pipeline {text "hello world from OTHER"}
let otherHelloWorld2 = pipeline {text "hello world from OTHER 2"}
let helloWorld = pipeline {text "hello world"}
let helloWorld2 = pipeline {text "hello world2"}
let helloWorldName str = pipeline {text ("hello world, " + str) }
let helloWorldNameAge (str, age) = pipeline {text (sprintf "hello world, %s, You're %i" str age) }


let apiRouter = scope {
    pipe_through apiHeaderPipe
    error_handler (text "Api 404")


    get "/" apiHelloWorld
    get "/a" apiHelloWorld2
}

let otherRouter = scope {
    pipe_through otherHeaderPipe
    error_handler (text "Other 404")

    get "/" otherHelloWorld
    get "/a" otherHelloWorld2
}

let router = scope {
    pipe_through headerPipe
    error_handler (text "404")


    get "/" helloWorld
    get "/a" helloWorld2
    getf "/name/%s" helloWorldName
    getf "/name/%s/%i" helloWorldNameAge

    forward "/other" otherRouter
    forward "/api" apiRouter
}

// let classicRouter =

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")

    let errorHandler (ex : Exception) (logger : ILogger) =
        logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
        clearResponse >=> Giraffe.HttpStatusCodeHandlers.ServerErrors.INTERNAL_ERROR ex.Message

    let configureApp (app : IApplicationBuilder) =
        app.UseGiraffeErrorHandler(errorHandler)
            .UseGiraffe router


    WebHost.CreateDefaultBuilder()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .UseUrls("http://0.0.0.0:8085/")
        .Build()
        .Run()

    0 // return an integer exit code

