(**
---
title: Controllers
category: explanation
menu_order: 7

---
*)
(*** hide ***)
#I "../../../temp/"
#I "../../../packages/docsasp/Microsoft.AspNetCore.app.ref/ref/net6.0"
#r "Saturn.dll"
#r "Giraffe.dll"
#r "Microsoft.AspNetCore.Http.Abstractions.dll"

(**
# Controller

In Saturn, a **controller** is a list of routes that is focused on a **model** (an object that contains your data). So if you have a user model, some common operations are to display the list of users, show details of a user, add a user, update or user, or remove a user. A controller is a great way to organize all of these actions.

Each of the operations is a separate route and a controller is an easy way to group these routes together.

A basic user controller is shown below:

*)
open Saturn

let userController = controller {
    index (fun ctx -> "Index handler version 1" |> Controller.text ctx) //View list of users
    add (fun ctx -> "Add handler version 1" |> Controller.text ctx) //Add a user
    create (fun ctx -> "Create handler version 1" |> Controller.text ctx) //Create a user
    show (fun ctx id -> (sprintf "Show handler version 1 - %i" id) |> Controller.text ctx) //Show details of a user
    edit (fun ctx id -> (sprintf "Edit handler version 1 - %i" id) |> Controller.text ctx)  //Edit a user
    update (fun ctx id -> (sprintf "Update handler version 1 - %i" id) |> Controller.text ctx)  //Update a user
}

(**
Here we can see the `index`, `add`, `create`, `show`, `edit`, and `update` operations but there are more operations that are not shown here like `patch` and `delete`. You can see all the operations int the [API Reference](../reference/Saturn/saturn-controller-controllerbuilder-10.html). You do not have to handle all of the operations.

You might be wondering what the difference is between `add` and `create` or `edit` and `update`. The `add` operation tells the application to return the form so that the user can enter the data for the user to be added. The `create` operation will commit the data to the database of the application. It is the same with `edit` for displaying the form and `update` for committing the change.

To add the controller for the routes, you can add it to the `defaultView` router like so:

*)

let defaultView = router {
    get "/" (htmlView Index.layout)
    get "/index.html" (redirectTo false "/")
    get "/default.html" (redirectTo false "/")
    forward "/users" userController
}

(**
The route will now be:

```bash
yoursite.com
└── "" (router)
    └── "" (browserRouter)
        └── "" (defaultView)
            ├── "/"                 -yoursite.com/
            ├── "/index.html"       -redirect to yoursite.com/
            ├── "/default.html"     -redirect to yoursite.com/
            └── "/users"
                ├── index "/"           -yoursite.com/users/
                ├── add "/add"          -yoursite.com/users/add
                ├── create              -POST yoursite.com/users/add
                ├── show "/%i"          -yoursite.com/users/%i
                ├── edit "/%i/edit"     -yoursite.com/users/%i/edit
                └── update ""           -POST yoursite.com/users/%i/edit
```

The create and update operations make changes to the database so you have to make a POST request containing the information you want to save to the database.

## Subcontroller

Now that you know how to chain routers together to create routes, we can look at a common scenario for a website. A website usually has users and each user can create multiple comments.

```bash
yoursite.com
└── "/users"
    └── "/%i"           -yoursite.com/users/%i
        └── "/comments" (commentController)
            ├── index "/"           -yoursite.com/users/{userId}/comments/
            └── show "/%i"          -yoursite.com/users/{userId}/comments/{commentId}
```

In Saturn, you can make the comment controller a subcontroller of the user controller. It looks like the following code:

*)

let commentController userId = controller {
    index (fun ctx -> (sprintf "Comment Index handler for user %i" userId ) |> Controller.text ctx)
    add (fun ctx -> (sprintf "Comment Add handler for user %i" userId ) |> Controller.text ctx)
    show (fun ctx id -> (sprintf "Show comment %s handler for user %i" id userId ) |> Controller.text ctx)
    edit (fun ctx id -> (sprintf "Edit comment %s handler for user %i" id userId )  |> Controller.text ctx)
}

let userController = controller {
    subController "/comments" commentController

    plug [All] (setHttpHeader "user-controller-common" "123")
    plug [Index; Show] (setHttpHeader "user-controller-specialized" "123")

    index (fun ctx -> "Index handler no version" |> Controller.text ctx)
    show (fun ctx id -> (sprintf "Show handler no version - %i" id) |> Controller.text ctx)
    add (fun ctx -> "Add handler no version" |> Controller.text ctx)
    create (fun ctx -> "Create handler no version" |> Controller.text ctx)
    edit (fun ctx id -> (sprintf "Edit handler no version - %i" id) |> Controller.text ctx)
    update (fun ctx id -> (sprintf "Update handler no version - %i" id) |> Controller.text ctx)
    delete (fun ctx id -> failwith (sprintf "Delete handler no version failed - %i" id) |> Controller.text ctx)
    error_handler (fun ctx ex -> sprintf "Error handler no version - %s" ex.Message |> Controller.text ctx)
}

(**

To create a subcontroller, start with creating a controller for your model. After that, define it as a subcontroller inside the main controller with the following code:

```fsharp
    subController "/yourModel" yourModelController
```


## API Reference

Full API reference for `controller` CE can be found [here](../reference/Saturn/saturn-controller-controllerbuilder-10.html)

Full API reference for `Controller` module containing useful helpers can be found [here](../reference/Saturn/saturn-controllerhelpers-controller.html)

*)
