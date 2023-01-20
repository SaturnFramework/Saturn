---
title: Saturn | CORSConfig
layout: standard
---

# CORSConfig

**Namespace:** [Saturn](./saturn.html)

**Parent:** [CORS](./saturn-cors.html)

The configuration values for CORS.

| Name             | Description                                                                                        | Implementation Link                                                                     |
|------------------|----------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------|
| Record Fields    |                                                                                                    |                                                                                         |
| `allowedUris`    | The list of allowed Uri(s) for requests.                                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CORS.fs#L39-39) |
| `allowedMethods` | The list of allowed HttpMethods for the request.                                                   | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CORS.fs#L41-41) |
| `allowCookies`   | Allow cookies? This is sent in the AccessControlAllowCredentials header.                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CORS.fs#L43-43) |
| `exposeHeaders`  | The list of response headers exposed to client. This is sent in AccessControlExposeHeaders header. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CORS.fs#L45-45) |
| `maxAge`         | Max age in seconds the user agent is allowed to cache the result of the request.                   | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/CORS.fs#L47-47) |
