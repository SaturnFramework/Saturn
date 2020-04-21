namespace Saturn

open Giraffe.Core
open System
open Microsoft.IdentityModel.Tokens
open System.Text
open System.IdentityModel.Tokens.Jwt
open Giraffe.Auth

[<AutoOpen>]
module ChallengeType =
  type ChallengeType =
    | JWT
    | Cookies
    | Custom of string

///Module containing types to be use with `use_oauth_with_settings`
module OAuth =

  ///Record type representing simple OAuth configuration to be used with `use_oauth_with_settings`.
  type OAuthSettings = {
    ///Name of the schema to be registered
    Schema: string
    ///OAuth CallbackPath endpoint
    CallbackPath: string
    ///OAuth Authorization endpoint
    ///For example: https://github.com/login/oauth/authorize
    AuthorizationEndpoint: string
    ///OAuth Token endpoint
    ///For example: https://github.com/login/oauth/access_token
    TokenEndpoint: string
    ///OAuth User Information endpoint
    ///For example: https://api.github.com/user
    UserInformationEndpoint: string
    ///Sequance of tuples where first element is a name of the of the key in JSON object and second element is a name of the claim.
    ///For example: `["login", "githubUsername"; "name", "fullName"]` where `login` and `name` are names of fields in GitHub JSON response (https://developer.github.com/v3/users/#get-the-authenticated-user).
    Claims : (string * string) seq
  }

///Module with some useful helpers functions that can be used for authentication, such as creating JWT tokens, or `HttpHandlers` checking if request is authenticated.
module Auth =
  let private mapChallengeTypeToScheme = function
    | JWT -> Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme
    | Cookies -> Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme
    | Custom s -> s

  ///Requires authentication and uses given challenge type if not authenticated
  let requireAuthentication challengeType : HttpHandler =
    requiresAuthentication (challenge (mapChallengeTypeToScheme challengeType))

  ///Requires role and uses given challenge type if not authenticated
  let requireRole challengeType role : HttpHandler =
    requiresRole role (challenge (mapChallengeTypeToScheme challengeType))

  ///Requires one of the roles and uses given challenge type if not authenticated
  let requireRoleOf challengeType roles : HttpHandler =
    requiresRoleOf roles (challenge (mapChallengeTypeToScheme challengeType))

  ///Requires claim of given type with given value and uses given challenge type if not authenticated
  let requireClaim challengeType claimKey claimValue : HttpHandler =
    authorizeUser (fun c -> c.HasClaim(claimKey, claimValue)) (challenge (mapChallengeTypeToScheme challengeType))

  ///Helper function to generate JWT token using `Microsoft.IdentityModel.Tokens` and `System.IdentityModel.Tokens.Jwt`
  let generateJWT (secret : string, algorithm) issuer expires claims =
    let expires = Nullable(expires)
    let notBefore = Nullable(DateTime.UtcNow)
    let securityKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    let signingCredentials = SigningCredentials(key = securityKey, algorithm = algorithm)

    let token = JwtSecurityToken(issuer, issuer, claims, notBefore, expires, signingCredentials )
    JwtSecurityTokenHandler().WriteToken token
