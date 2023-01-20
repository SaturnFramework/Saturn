---
title: Saturn | Auth
layout: standard
---

# Auth

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Saturn](./saturn.html)

Module with some useful helpers functions that can be used for authentication, such as creating JWT tokens, or `HttpHandlers` checking if request is authenticated.

**Values and Functions**

| Name                                                   | Description                                                                                                         | Implementation Link                                                                               |
|--------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------|
| `requireAuthentication(challengeType)`                 | Requires authentication and uses given challenge type if not authenticated.                                         | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L48-48) |
| `requireRole challengeType role`                       | Requires role and uses given challenge type if not authenticated.                                                   | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L52-52) |
| `requireRoleOf challengeType roles`                    | Requires one of the roles and uses given challenge type if not authenticated.                                       | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L56-56) |
| `requireClaim challengeType claimKey claimValue`       | Requires claim of given type with given value and uses given challenge type if not authenticated.                   | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L60-60) |
| `generateJWT(secret, algorithm) issuer expires claims` | Helper function to generate JWT token using `Microsoft.IdentityModel.Tokens` and `System.IdentityModel.Tokens.Jwt`. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L64-64) |
