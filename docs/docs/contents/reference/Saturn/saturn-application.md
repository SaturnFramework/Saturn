---
title: Saturn | Application
layout: standard
---

# Application

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Saturn](./saturn.html)

Module containing `application` computation expression.

**Declared Types**

* **Type:** [ApplicationBuilder](./saturn-application-applicationbuilder.html)

  **Description:** Computation expression used to configure Saturn application. Under the hood it's using ASP.NET application configurations interfaces such as `IWebHostBuilder`, `IServiceCollection`, `IApplicationBuilder` and others. It aims to hide cumbersome ASP.NET application configuration and enable high level, declarative application configuration using feature toggles.

    **Example:**

    ```fsharp
    let app = application {
        pipe_through endpointPipe
        use_router topRouter
        url "http://0.0.0.0:8085/"
        memory_cache
        use_static "static"
        use_gzip
    }
    ```

---

* **Type:** [ApplicationState](./saturn-application-applicationstate.html)

  **Description:** Type representing internal state of the `application` computation expression.

---

**Declared Modules**

| Module                                               | Description                           |
|------------------------------------------------------|---------------------------------------|
| [Config](./saturn-application-config.html)           | Helpers for getting configuration.    |
| [Environment](./saturn-application-environment.html) | Helpers for getting environment info. |

**Values and Functions**

| Name                               | Description                                                                      | Implementation Link                                                                              |
|------------------------------------|----------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------|
| `parseAndValidateOauthTicket(ctx)` | Generic oauth parse and validate logic, shared with the auth extensions package. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Application.fs#L57-57)   |
| `application`                      | Computation expression used to configure Saturn application.                     | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Application.fs#L632-632) |
| `run(app)`                         | Runs Saturn application.                                                         | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Application.fs#L635-635) |
