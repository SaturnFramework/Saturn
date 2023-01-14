---
title: Saturn | Saturn
layout: standard
---

# Saturn

**Namespace:** [global](./global.html)

**Parent:** [global](./global.html)

**Type Extensions**

| Name                                              | CE Custom Operation        | Description                                                                                                                                                                                                                                                                                                                                                    | Implementation Link                                                                                                  |
|---------------------------------------------------|----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| `x.UseHttpSys(state)`                             | `use_httpsys`              | HTTP.sys is a web server for ASP.NET Core that only runs on Windows. HTTP.sys is an alternative to Kestrel server and offers some features that Kestrel doesn't provide. ([link](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys)) This operation switches hosting to the HTTP.sys server.                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.Extensions.HttpSys/Saturn.HttpSys.fs#L14-14) |
| `x.UseHttpSysWithConfig(state, config)`           | `use_httpsys_with_config`  | HTTP.sys is a web server for ASP.NET Core that only runs on Windows. HTTP.sys is an alternative to Kestrel server and offers some features that Kestrel doesn't provide. ([link](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys)) This operation switches hosting to the HTTP.sys server and takes additional config.               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.Extensions.HttpSys/Saturn.HttpSys.fs#L25-25) |
| `x.UserHttpSysWindowsAuth(state, allowAnonymous)` | `use_httpsys_windows_auth` | HTTP.sys is a web server for ASP.NET Core that only runs on Windows. HTTP.sys is an alternative to Kestrel server and offers some features that Kestrel doesn't provide. ([link](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys)) This operation switches hosting to the HTTP.sys server and enables Windows Auth (NTLM/Negotiate). | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn.Extensions.HttpSys/Saturn.HttpSys.fs#L36-36) |
