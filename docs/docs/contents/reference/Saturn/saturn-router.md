---
title: Saturn | Router
layout: standard
---

# Router

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Saturn](./saturn.html)

Module containing `pipeline` computation expression.

**Declared Modules**

* **Type:** [RouteType](./saturn-router-routetype.html)

  **Description**: Type representing route type, used in internal state of the `application` computation expression.

---

* **Type:** [RouterBuilder](./saturn-router-routerbuilder.html)

  **Description**: Computation expression used to create routing, combining `HttpHandlers`, `pipelines` and `controllers` together.

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

---

* **Type:** [RouterState](./saturn-router-routerstate.html.html)

  **Description:** Type representing internal state of the `router` computation expression.

---

**Values and Functions**

| Name     | Description                                                          | Implementation Link                                                                         |
|----------|----------------------------------------------------------------------|---------------------------------------------------------------------------------------------|
| `router` | Computation expression used to create routing in Saturn application. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Router.fs#L326-326) |
