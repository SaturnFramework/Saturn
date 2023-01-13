---
title: Endpoint Routing
layout: standard
---

# Endpoint Routing

Saturn from version `0.15` supports [ASP.NET Endpoint Routing](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-3.1) additionally to custom routing implementation provided by Giraffe. The main difference between old routing and new one is that Endpoint Routing assumes total separation of routing from behavior - it works by statically defining list of all possible routes on startup of application. This impacts design of some of our Saturn abstractions, and means new routing is not 100% compatible with old one. On the bright side, theoretically, Endpoint Routing should provide noticeable performance boost, and in the future it will allow for better ecosystem integration.

> Currently endpoint routing API is treated as an experimental API - it's subject to changes.

To use Endpoint Routing you need to open `Saturn.Endpoint` namespace - this will override known `router` and `controller` Computation Expressions with their Endpoint Routing versions. In `application` CE instead of using `use_router` operation you should use `use_endpoint_router` operation. For many simple applications this may be enough to get things working - we've been trying to keep API as compatible as possible.

However there are differences:

* With Endpoint Routing `router` and `controller` computation expressions are not transformed to `HttpHandler` but rather to `Endpoint list`. This has a huge impact on composability of those abstractions - you can't do things like `myHttpHandler >=> router { ... }` any more. Such code should be replaced with `plug/pipe_through` functionality in both `router` and `controller`. `Endpoint list` can basically be used in 2 places - in `forward` operation in `router` and `use_endpoint_router` in `application`.
* Lack of `subroutef` in Giraffe-EndpointRouting - because EndpointRouting needs to have all possible route templates at the application startup it's really hard to emulate some of previous Giraffe's routing composibility capabilities. From Saturn point of view this created 2 major changes:
  - there's no `forwardf` in `router` CE anymore - it should be replaced with set of `getf/postf/putf ... ` operations in child router.
  - `subController` in `controller` CE doesn't work well in Endpoint Routing - you can use as subcontrollers only old, HttpHandler based controllers (even if you parent controller is Endpoint Routing controller). In `Saturn.Endpoint` we provide additonal `subcontroller` CE - it's an alias to old `controller` CE
* Lack of `case_insensitive` in `router` and `controller` - with Endpoint Routing all routes are case insensitive by default and there's no easy way to change it
* Lack of `not_found_handler` in `router` and `controller` - as Endpoint Routing creates global table of routing having scoped not-found handlers is really tricky. Use built-in ways of handle 404 in ASP.NET (such as `UseDeveloperExceptionPage`)
* Unlike Giraffe routing, Endpoint Routing doesn't ensure order of routing checks - this shouldn't be a problem in most cases, but I can imagine some edge cases in which this would matter.

In general, to reiterate - Endpoint Routing API in Giraffe/Saturn is still experimental. However, it probably is a future of Giraffe/Saturn so, if possible, please check if your applications can move to the Endpoint Routing API, and try it out. It's important for everyone involved to get feedback on this new routing engine.
