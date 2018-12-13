module WindowsAuthSample

open Saturn
open Giraffe.ResponseWriters
open Giraffe.Core
open FSharp.Control.Tasks.V2
open Saturn.Channels



let browserRouter = router {
    forward "" (text "Hello world")
}

let sampleChannel = channel {
    join (fun ctx -> task {return Ok})

    handle "topic" (fun ctx res msg ->
        task {
            printfn "%A" msg
            return ()
        }
    )
}


let app = application {
    use_router browserRouter
    url "http://localhost:8085/"
    add_channel "/channel" sampleChannel
}

[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code
