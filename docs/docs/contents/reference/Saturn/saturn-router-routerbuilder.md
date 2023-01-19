---
title: Saturn | RouterBuilder
layout: standard
---

# RouterBuilder

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Router](./saturn-router.html)

Computation expression used to create routing, combining `HttpHandlers`, `pipelines` and `controllers` together.

The result of the computation expression is a standard Giraffe `HttpHandler`, which means that it's easily composable with other parts of the ecosytem.

**Example:**

```fsharp
let topRouter = router {
    pipe_through headerPipe
    not_found_handler (text "404")

    get "/" helloWorld
    get "/a" helloWorld2
    getf "/name/%s" helloWorldName
    getf "/name/%s/%i" helloWorldNameAge

    //routers can be defined inline to simulate `subRoute` combinator
    forward "/other" (router {
        pipe_through otherHeaderPipe
        not_found_handler (text "Other 404")

        get "/" otherHelloWorld
        get "/a" otherHelloWorld2
    })

    // or can be defined separatly and used as HttpHandler
    forward "/api" apiRouter

    // same with controllers
    forward "/users" userController
}
```

| Name                                | CE Custom Operation | Description                                                                               | Implementation Link                                                                         |
|-------------------------------------|---------------------|-------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| Instance Members                    |                     |                                                                                           |                                                                                             |
| `x.CaseInsensitive(state)`          | `case_insensitive`  | Toggle case insensitve routing.                                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L322-322) |
| `x.Delete(state, path, action)`     | `delete`            | Adds handler for `DELETE` request.                                                        | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L282-282) |
| `x.DeleteF(state, path, action)`    | `deletef`           | Adds handler for `DELETE` request.                                                        | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L287-287) |
| `x.Forward(state, path, action)`    | `forward`           | Forwards calls to different `scope`. Modifies the `HttpRequest.Path` to allow subrouting. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L302-302) |
| `x.Forwardf(state, path, action)`   | `forwardf`          | Forwards calls to different `scope`. Modifies the `HttpRequest.Path` to allow subrouting. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L307-307) |
| `x.Get(state, path, action)`        | `get`               | Adds handler for `GET` request.                                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L252-252) |
| `x.GetF(state, path, action)`       | `getf`              | Adds handler for `GET` request.                                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L257-257) |
| `x.NotFoundHandler(state, handler)` | `not_found_handler` | Adds not-found handler for current scope.                                                 | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L317-317) |
| `x.Patch(state, path, action)`      | `patch`             | Adds handler for `PATCH` request.                                                         | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L292-292) |
| `x.PatchF(state, path, action)`     | `patchf`            | Adds handler for `PATCH` request.                                                         | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L297-297) |
| `x.PipeThrough(state, pipe)`        | `pipe_through`      | Adds pipeline to the list of pipelines that will be used for every request.               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L312-312) |
| `x.Post(state, path, action)`       | `post`              | Adds handler for `POST` request.                                                          | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L262-262) |
| `x.PostF(state, path, action)`      | `postf`             | Adds handler for `POST` request.                                                          | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L267-267) |
| `x.Put(state, path, action)`        | `put`               | Adds handler for `PUT` request.                                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L272-272) |
| `x.PutF(state, path, action)`       | `putf`              | Adds handler for `PUT` request.                                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L277-277) |
| `x.Run(state)`                      |                     |                                                                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L105-105) |
| `x.Yield(arg1)`                     |                     |                                                                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L98-98)   |
