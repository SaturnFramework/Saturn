module JWTSample

open System
open Saturn
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens
open Giraffe
open FSharp.Control.Tasks.V2.ContextInsensitive
open Microsoft.AspNetCore.Http

//Based on https://medium.com/@dsincl12/json-web-token-with-giraffe-and-f-4cebe1c3ef3b

let secret = "spadR2dre#u-ruBrE@TepA&*Uf@U"
let issuer = "saturnframework.io"

[<CLIMutable>]
type LoginViewModel =
    {
        Email : string
        Password : string
    }

[<CLIMutable>]
type TokenResult =
    {
        Token : string
    }

let generateToken email =
    let claims = [|
        Claim(JwtRegisteredClaimNames.Sub, email);
        Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) |]
    claims
    |> Auth.generateJWT (secret, SecurityAlgorithms.HmacSha256) issuer (DateTime.UtcNow.AddHours(1.0))

let handleGetSecured =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let email = ctx.User.FindFirst ClaimTypes.NameIdentifier
        text ("User " + email.Value + " is authorized to access this resource.") next ctx

let handlePostToken =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindJsonAsync<LoginViewModel>()

            // authenticate user

            let tokenResult = generateToken model.Email

            return! json tokenResult next ctx
}

let securedRouter = router {
    with_auth (Auth.requireAuthentication JWT)
    get "/" handleGetSecured
}

let topRouter = router {
    not_found_handler (setStatusCode 404 >=> text "Not Found")

    post "/token" handlePostToken
    get "/" (text "public route")
    forward "/secured" securedRouter
}

let app = application {
    use_jwt_authentication secret issuer

    use_router topRouter
    url "http://0.0.0.0:8085/"
}

[<EntryPoint>]
let main _ =
    run app
    0
