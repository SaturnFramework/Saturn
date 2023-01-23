---
title: Saturn | ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput>
layout: standard
---

# ControllerBuilder<'Key, 'IndexOutput, 'ShowOutput, 'AddOutput, 'EditOutput, 'CreateOutput, 'UpdateOutput, 'PatchOutput, 'DeleteOutput, 'DeleteAllOutput>

**Namespace:** [Saturn](./saturn.html)

**Parent:** [Controller](./saturn-controller.html)

Computation expression used to create Saturn controllers - abstraction representing REST-ish endpoint for serving HTML views or returning data. It supports:

* a set of predefined actions that are automatically mapped to the endpoints following standard conventions
* embedding sub-controllers for modeling one-to-many relationships
* versioning
* adding plugs for a particular action which in principle provides the same mechanism as attributes in ASP.NET MVC applications
* defining a common error handler for all actions
* defining a not-found action

The result of the computation expression is a standard Giraffe `HttpHandler`, which means that it's easily composable with other parts of the ecosytem.

**Example:**

```fsharp
let commentController userId = controller {
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

| Name | CE Custom Operation | Description | Implementation Link |
|-|-|-|-|
| Instance Members |
| `x.Add(state, handler)` | `add` | Operation that should render form for adding new item. | [link](https://github.com/SaturnFramework/Saturn/blob/main/src/Saturn/Controller.fs#L123) |
| `x.CaseInsensitive(state)` | `case_insensitive` | Toggle case insensitve routing. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L209-209) |
| `x.Create(state, handler)` | `create` | Operation that creates new item. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L146-146) |
| `x.Delete(state, handler)` | `delete` | Operation that deletes existing item. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L170-170) |
| `x.DeleteAll(state, handler)` | `delete_all` | Operation that deletes all items. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L178-178) |
| `x.Edit(state, handler)` | `edit` | Operation that should render form for editing existing item. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L138-138) |
| `x.ErrorHandler(state, handler)` | `error_handler` | Define error for the controller. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L191-191) |
| `x.Index(state, handler)` | `index` | Operation that should render (or return in case of API controllers) list of data. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L114-114) |
| `x.NotFoundHandler(state, handler)` | `not_found_handler` | Define not-found handler for the controller. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L186-186) |
| `x.Patch(state, handler)` | `patch` | Operation that patches existing item. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L162-162) |
| `x.Plug(state, actions, handler)` | `plug` | Add a plug that will be run on each of the provided actions. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L219-219) |
| `x.Run(state)` | | | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L327-327) |
| `x.Show(state, handler)` | `show` | Operation that should render (or return in case of API controllers) single entry of data. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L122-122) |
| `x.SubController(state, route, handler)` | `subController` | Inject a controller into the routing table rooted at a given route. All of that controller's actions will be anchored off of the route as a prefix. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L214-214) |
| `x.Update(state, handler)` | `update` | Operation that updates existing item. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L154-154) |
| `x.Version(state, version)` | `version` | Define version of controller. Adds checking of `x-controller-version` header. | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L204-204) |
| `x.Yield(arg1)` | | | [link](https://github.com/SaturnFramework/Saturn/tree/master/src/Saturn/Controller.fs#L109-109) |