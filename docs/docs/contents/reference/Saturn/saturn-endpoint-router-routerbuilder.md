---
title: Saturn | RouterBuilder
layout: standard
---

# RouterBuilder

**Namespace:** [Saturn.Endpoint](./saturn.endpoint.html)

**Parent:** [Router](./saturn-endpoint-router.html)

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

| Name                              | CE Custom Operation | Description                                                                 | Implementation Link                                                                                 |
|-----------------------------------|---------------------|-----------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| Instance Members                  |                     |                                                                             |                                                                                                     |
| `x.Delete(state, path, action)`   | `delete`            | Adds handler for `DELETE` request.                                          | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L201-201) |
| `x.DeleteF(state, path, action)`  | `deletef`           | Adds handler for `DELETE` request.                                          | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L206-206) |
| `x.Forward(state, path, actions)` |                     | Forwards calls to different list of `Endpoint`.                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L225-225) |
| `x.Forward(state, path, action)`  | `forward`           | Forwards calls to different `Endpoint`.                                     | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L221-221) |
| `x.Get(state, path, action)`      | `get`               | Adds handler for `GET` request.                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L171-171) |
| `x.GetF(state, path, action)`     | `getf`              | Adds handler for `GET` request.                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L176-176) |
| `x.Patch(state, path, action)`    | `patch`             | Adds handler for `PATCH` request.                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L211-211) |
| `x.PatchF(state, path, action)`   | `patchf`            | Adds handler for `PATCH` request.                                           | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L216-216) |
| `x.PipeThrough(state, pipe)`      | `pipe_through`      | Adds pipeline to the list of pipelines that will be used for every request. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L230-230) |
| `x.Post(state, path, action)`     | `post`              | Adds handler for `POST` request.                                            | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L181-181) |
| `x.PostF(state, path, action)`    | `postf`             | Adds handler for `POST` request.                                            | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L186-186) |
| `x.Put(state, path, action)`      | `put`               | Adds handler for `PUT` request.                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L191-191) |
| `x.PutF(state, path, action)`     | `putf`              | Adds handler for `PUT` request.                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L196-196) |
| `x.Run(state)`                    |                     |                                                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L109-109) |
| `x.Yield(arg1)`                   |                     |                                                                             | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/RouterEndpoint.fs#L103-103) |
