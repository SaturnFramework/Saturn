---
title: Saturn | OAuth
layout: standard
---

# OAuthSettings

**Namespace:** [Saturn](./saturn.html)

**Parent:** [OAuth](./saturn-oauth.html)

Record type representing simple OAuth configuration to be used with `use_oauth_with_settings`.

| Name                      | Description                                                                                                                                                                                                                                                                                                                           | Implementation Link                                                                               |
|---------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------|
| Record Fields             |                                                                                                                                                                                                                                                                                                                                       |                                                                                                   |
| `Schema`                  | Name of the schema to be registered.                                                                                                                                                                                                                                                                                                  | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L23-23) |
| `CallbackPath`            | OAuth CallbackPath endpoint.                                                                                                                                                                                                                                                                                                          | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L25-25) |
| `AuthorizationEndpoint`   | OAuth Authorization endpoint For example: https://github.com/login/oauth/authorize.                                                                                                                                                                                                                                                   | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L28-28) |
| `TokenEndpoint`           | OAuth Token endpoint For example: https://github.com/login/oauth/access_token.                                                                                                                                                                                                                                                        | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L31-31) |
| `UserInformationEndpoint` | OAuth User Information endpoint For example: https://api.github.com/user.                                                                                                                                                                                                                                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L34-34) |
| `Claims`                  | Sequance of tuples where first element is a name of the of the key in JSON object and second element is a name of the claim. For example: `["login", "githubUsername"; "name", "fullName"]` where `login` and `name` are names of fields in GitHub JSON response (https://developer.github.com/v3/users/#get-the-authenticated-user). | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Authentication.fs#L37-37) |
