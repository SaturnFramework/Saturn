---
title: Saturn | Controller
layout: standard
---

# Controller

**Namespace:** [Saturn.Endpoint](./saturn.endpoint.html)

**Parent:** [Saturn.Endpoint](./saturn.endpoint.html)

Module with `controller` computation expression.

**Declared Types**

---

* **Type:** [Action](./saturn-endpoint-controller-action.html)

  **Description:** Type used for `plug` operation, allowing you to choose for which actions given plug should work.

---

* **Type:** [ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput>](./saturn-endpoint-controller-controllerbuilder-10.html)

  **Description:** Computation expression used to create Saturn controllers - abstraction representing REST-ish endpoint for serving HTML views or returning data. It supports:

    * a set of predefined actions that are automatically mapped to the endpoints following standard conventions
    * embedding sub-controllers for modeling one-to-many relationships
    * versioning
    * adding plugs for a particular action which in principle provides the same mechanism as attributes in ASP.NET MVC applications
    * defining a common error handler for all actions
    * defining a not-found action

    The result of the computation expression is a standard Giraffe `HttpHandler`, which means that it's easily composable with other parts of the ecosytem.

    **Example:**

    ```fsharp
    let commentController userId = subcontroller {
        index (fun ctx -> (sprintf "Comment Index handler for user %i" userId ) |> Controller.text ctx)
        add (fun ctx -> (sprintf "Comment Add handler for user %i" userId ) |> Controller.text ctx)
        show (fun (ctx, id) -> (sprintf "Show comment %s handler for user %i" id userId ) |> Controller.text ctx)
        edit (fun (ctx, id) -> (sprintf "Edit comment %s handler for user %i" id userId )  |> Controller.text ctx)
    }

    let userControllerVersion1 = controller {
        version 1
        subController "/comments" commentController

        index (fun ctx -> "Index handler version 1" |> Controller.text ctx)
        add (fun ctx -> "Add handler version 1" |> Controller.text ctx)
        show (fun (ctx, id) -> (sprintf "Show handler version 1 - %i" id) |> Controller.text ctx)
        edit (fun (ctx, id) -> (sprintf "Edit handler version 1 - %i" id) |> Controller.text ctx)
    }

    let userController = controller {
        subController "/comments" commentController

        plug [All] (setHttpHeader "user-controller-common" "123")
        plug [Index; Show] (setHttpHeader "user-controller-specialized" "123")

        index (fun ctx -> "Index handler no version" |> Controller.text ctx)
        add (fun ctx -> "Add handler no version" |> Controller.text ctx)
        show (fun (ctx, id) -> (sprintf "Show handler no version - %i" id) |> Controller.text ctx)
        edit (fun (ctx, id) -> (sprintf "Edit handler no version - %i" id) |> Controller.text ctx)
    }
    ```

---

* **Type:** [ControllerState<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput>](./saturn-endpoint-controller-controllerstate-10.html)

  **Description:** Type representing internal state of the `controller` computation expression.

---

**Values and Functions**

| Name                 | Description                                                                     | Implementation Link                                                                                     |
|----------------------|---------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| `except(actions)`    | Returns list of all actions except given actions.                               | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerEndpoint.fs#L32-32)   |
| `response ctx input` |                                                                                 | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerEndpoint.fs#L58-58)   |
| `controller`         | Computation expression used to create controllers.                              | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerEndpoint.fs#L472-472) |
| `subcontroller`      | Computation expression used to create HttpHandlers representing subcontrollers. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/ControllerEndpoint.fs#L475-475) |
