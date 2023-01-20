---
title: Saturn | Pipeline
layout: standard
---

# Pipeline

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Saturn](./saturn.html)

Module containing `pipeline` computation expression.

**Declared Types**

* **Type:** [PipelineBuilder](./saturn-pipeline-pipelinebuilder.html)

  **Description:** Computation expression used to combine `HttpHandlers` in a declarative manner.

    The result of the computation expression is a standard Giraffe `HttpHandler` which means that it's easily composable with other parts of the Giraffe ecosystem.

    **Example:**

    ```fsharp
    let headerPipe = pipeline {
        set_header "myCustomHeader" "abcd"
        set_header "myCustomHeader2" "zxcv"
    }

    let endpointPipe = pipeline {
        plug fetchSession
        plug head
        plug requestId
    }
    ```

---

**Values and Functions**

| Name       | Description                                                                                                         | Implementation Link                                                                            |
|------------|---------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|
| `pipeline` | `pipeline` computation expression is a way to create `HttpHandler` using composition of low-level helper functions. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Pipelines.fs#L154-154) |
