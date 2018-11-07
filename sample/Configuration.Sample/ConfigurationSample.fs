module ConfigurationSample

open System
open Saturn
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Microsoft.IdentityModel.Tokens
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Authentication.JwtBearer

//Based on https://medium.com/@dsincl12/json-web-token-with-giraffe-and-f-4cebe1c3ef3b

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

[<CLIMutable>]
type AuthSettings =
    {
        Secret : string
        Issuer : string
    }

let generateToken authSettings email =
    let claims = [|
        Claim(JwtRegisteredClaimNames.Sub, email);
        Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) |]
    claims
    |> Auth.generateJWT (authSettings.Secret, SecurityAlgorithms.HmacSha256) authSettings.Issuer (DateTime.UtcNow.AddHours(1.0))

let handleGetSecured =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        let email = ctx.User.FindFirst ClaimTypes.NameIdentifier
        text ("User " + email.Value + " is authorized to access this resource.") next ctx

let getAuthSettings (configuration : IConfiguration) =
  let settings = configuration.GetSection("Auth").Get<AuthSettings>()
  settings

let handlePostToken =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindJsonAsync<LoginViewModel>()

            // authenticate user
            let configuration = ctx.GetService<IConfiguration>()
            let tokenResult = generateToken (getAuthSettings configuration) model.Email

            return! json tokenResult next ctx
}

let securedRouter = router {
    pipe_through (Auth.requireAuthentication JWT)
    get "/" handleGetSecured
}

let topRouter = router {
    not_found_handler (setStatusCode 404 >=> text "Not Found")

    post "/token" handlePostToken
    get "/" (text "public route")
    forward "/secured" securedRouter
}

let configureJwt (configuration : IConfiguration) (options : JwtBearerOptions) =
  let authSettings = getAuthSettings configuration
  let tvp = TokenValidationParameters()
  tvp.ValidateActor <- true
  tvp.ValidateAudience <- true
  tvp.ValidateLifetime <- true
  tvp.ValidateIssuerSigningKey <- true
  tvp.ValidIssuer <- authSettings.Issuer
  tvp.ValidAudience <- authSettings.Issuer
  tvp.IssuerSigningKey <- SymmetricSecurityKey(Text.Encoding.UTF8.GetBytes authSettings.Secret)
  options.TokenValidationParameters <- tvp

let app = application {
    use_jwt_auth_from_configuration configureJwt

    use_router topRouter
    url "http://0.0.0.0:8085/"
}

[<EntryPoint>]
let main _ =
    run app
    0
