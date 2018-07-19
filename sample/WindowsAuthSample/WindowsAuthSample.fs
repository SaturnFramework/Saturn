module WindowsAuthSample

open Saturn
open Giraffe.ResponseWriters

let app = application {
    use_router (fun f c ->
                let message = sprintf "hello %s!" c.User.Identity.Name
                text message f c)
    url "http://localhost:8085/"
    memory_cache
    use_gzip
    disable_diagnostics
    use_httpsys_windows_auth false
}

[<EntryPoint>]
let main _ =
    run app
    0 // return an integer exit code
