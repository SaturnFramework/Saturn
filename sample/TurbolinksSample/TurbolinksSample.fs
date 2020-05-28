module WindowsAuthSample

open Saturn
open Giraffe.ResponseWriters
open Giraffe.Core

let browser = pipeline {
    plug acceptHtml
    plug putSecureBrowserHeaders
    set_header "x-pipeline-type" "Browser"
}

let defaultView = router {
    //TODO
    // get "/" (htmlView Index.layout)
    get "/index.html" (redirectTo false "/")
    get "/default.html" (redirectTo false "/")
}

let browserRouter = router {
    pipe_through browser

    forward "" defaultView
    //TODO
    // forward "/otherView" (htmlView OtherView.layout)
}


let app = application {
    use_router browserRouter
    url "http://localhost:8085/"
    use_static "static"
    use_turbolinks
}

[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code
