namespace Saturn

open Giraffe.HttpHandlers
open System
open Microsoft.IdentityModel.Tokens
open System.Text
open System.IdentityModel.Tokens.Jwt

[<AutoOpen>]
module ChallengeType =
  type ChallengeType =
    | JWT
    | Cookies
    | GitHub
    | Custom of string

module Auth =
  let private mapChallengeTypeToScheme = function
    | JWT -> Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme
    | Cookies -> Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme
    | GitHub -> "Github"
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

  //Requires claim of given type with given value and uses given challenge type if not authenticated
  let requireClaim challengeType claimKey claimValue : HttpHandler =
    requiresAuthPolicy (fun c -> c.HasClaim(claimKey, claimValue)) (challenge (mapChallengeTypeToScheme challengeType))

  let generateJWT (secret : string, algorithm) issuer expires claims =
    let expires = Nullable(expires)
    let notBefore = Nullable(DateTime.UtcNow)
    let securityKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    let signingCredentials = SigningCredentials(key = securityKey, algorithm = algorithm)

    let token = JwtSecurityToken(issuer, issuer, claims, notBefore, expires, signingCredentials )
    JwtSecurityTokenHandler().WriteToken token