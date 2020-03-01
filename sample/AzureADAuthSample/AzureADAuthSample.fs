module AzureADAuthSample

open Giraffe
open Saturn

let loggedIn = pipeline {
  requires_authentication (Giraffe.Auth.challenge "AzureAD")
}

let top = router {
  pipe_through loggedIn
  get "/" (fun next ctx -> 
      (ctx.User.Identity.Name |> json) next ctx)
}

// Find these information in the App Registration part of you Azure Active Directory (http://aka.ms/AppRegistrations).
let tenantId = "<your tenant id>"
let clientId = "<your client id>"
let clientSecred = "<your client secred>"
let callbackPath = "/auth"
let scopes =  [ "User.Read" ]
let tokenMappings = [ "name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" ]

let app = application {
  use_router top
  url "http://[::]:8085/"
  memory_cache
  use_gzip
  use_azuread_oauth tenantId clientId clientSecred callbackPath scopes tokenMappings
}

[<EntryPoint>]
let main _ =
  run app
  0 // return an integer exit code
