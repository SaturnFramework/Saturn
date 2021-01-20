(**
---
title: Router
category: explanation
menu_order: 5

---
*)
(*** hide ***)
#I "../../../temp/"
#I "../../../packages/docsasp/Microsoft.AspNetCore.app.ref/ref/netcoreapp3.1"
#r "Saturn.dll"
#r "Giraffe.dll"
#r "Microsoft.AspNetCore.Http.Abstractions.dll"

module Index =
  open Giraffe.GiraffeViewEngine
  let layout = div [] []

module NotFound =
  open Giraffe.GiraffeViewEngine
  let layout = div [] []

let someScopeOrController = Giraffe.ResponseWriters.text ""

(**
# Routing

Routes are how Saturn connects all the HTTP requests to the different actions. Think of a route as the URL of the application. The site is yoursite.com but you may have a route for your about page such as yoursite.com/about.

In Saturn, `Routers` contain all the routes of your application. A router is a list of routes. A website can have a router that handles the different routes to your page like so:

```bash
yoursite.com
├── "/"             -yoursite.com/
├── "/about"        -yoursite.com/about
├── "/contact"      -yoursite.com/contact
├── "/news"         -yoursite.com/news
└── "/investors"    -yoursite.com/investors
```

Since this is at the root, this is your router at `""` path. You can then add a router inside another router to have the following:

```bash
yoursite.com
├── books           -yoursite.com/books
|   ├── list        -yoursite.com/books/list
|   ├── add         -yoursite.com/books/add
|   ├── update      -yoursite.com/books/update
|   └── delete      -yoursite.com/books/update
├── about           -yoursite.com/about
├── contact         -yoursite.com/contact
├── news            -yoursite.com/news
└── investors       -yoursite.com/investors
```

Now you have a router for the `"/books"` path inside another router for the `""` path.

Now to see it in code, create a Saturn project from the template and you will have a `Router.fs` file like this:

*)


open Saturn
open Giraffe.Core
open Giraffe.ResponseWriters


let browser = pipeline {
    plug acceptHtml
    plug putSecureBrowserHeaders
    plug fetchSession
    set_header "x-pipeline-type" "Browser"
}

let defaultView = router {
    get "/" (htmlView Index.layout)
    get "/index.html" (redirectTo false "/")
    get "/default.html" (redirectTo false "/")
}

let browserRouter = router {
    not_found_handler (htmlView NotFound.layout) //Use the default 404 webpage
    pipe_through browser //Use the default browser pipeline

    forward "" defaultView //Use the default view
}

//Other scopes may use different pipelines and error handlers

// let api = pipeline {
//     plug acceptJson
//     set_header "x-pipeline-type" "Api"
// }

// let apiRouter = router {
//     not_found_handler (setStatusCode 404 >=> text "Api 404")
//     pipe_through api
//
//     forward "/someApi" someScopeOrController
// }

let appRouter = router {
    // forward "/api" apiRouter
    forward "" browserRouter
}

(**

First, take a look at the `router` function.

*)

let appRouter' = router {
    forward "" browserRouter
}

(**

The `appRouter` value is a `router`. Inside is the `forward "" browserRouter` line. The `forward` function needs a path and a router. In this case, the path is an empty string and the router is `browserRouter`. That means that the `browserRouter` router will handle the routes at the current location. Since `appRouter` is the first router called, the current location is the root of the application.

Now let's look at `browserRouter`:

*)

let browserRouter' = router {
    not_found_handler (htmlView NotFound.layout)
    pipe_through browser

    forward "" defaultView
}

(**

There are three lines. The first line, `not_found_handler (htmlView NotFound.layout)` tells `browserRouter` to display a not found page if the user enters a route that the application does not handle. The second line tells the application to use the `browser` pipeline defined above. The pipeline is a list of settings on how the website will deliver the pages. Lastly, `forward "" defaultView` is like `forward "" browserRouter` from the `appRouter`. Again, `browserRouter` does not contain any routes but it tells the `defaultView` router to handle them. Finally, we get to the part where the application is told how to handle the routes. Inside `defaultView`, we created 3 routes:

*)

let defaultView' = router {
    get "/" (htmlView Index.layout)
    get "/index.html" (redirectTo false "/")
    get "/default.html" (redirectTo false "/")
}

(**

Here, we see that `get` is used to define the routes. There are 3 routes here but 2 of them redirect to the first route. To illustrate, the routes are:

```bash
yoursite.com
└── "" (router)
    └── "" (browserRouter)
        └── "" (defaultView)
            ├── "/"                 -yoursite.com/
            ├── "/index.html"       -redirect to yoursite.com/
            └── "/default.html"     -redirect to yoursite.com/
```

Looking at the first line inside `defaultView`, `get "/" (htmlView Index.layout)` tells the application to display `Index.layout` at the root of the application. The `get` corresponds to the HTTP verb GET so when you type in a link, the browser tries to GET the page. The first parameter of `get` is "/", so basically when getting the root, the `get` function will return something. The second parameter is `(htmlView Index.layout)` so the `get` function returns an HTML page specified by Index.layout. The second and third line have `(redirectTo false "/")`, telling the application to go to "yoursite.com/" when going to "yoursite.com/index" or "yoursite.com/default"

## Best Practices

You can combine all 3 routers into one router like so:

*)


let appRouter'' = router {
    not_found_handler (htmlView NotFound.layout)
    pipe_through browser

    get "/" (htmlView Index.layout)
    get "/index.html" (redirectTo false "/")
    get "/default.html" (redirectTo false "/")
}

(**

The template splits them into 3 to encourage good practices. In the first router, you can see the commented out code `forward "/api" apiRouter`. This is a good suggestion in the template to have a separate router to handle your API routes. We set up how to deliever the webpage with `pipe_through browser` in `browserRouter`. The settings are important for a browser to know how to handle your routes but not for a different application to access your routes as an API.

The template provides an example of how to set up the API routes in the commented out code, which I copied below:

*)

let api = pipeline {
    plug acceptJson
    set_header "x-pipeline-type" "Api"
}

let apiRouter = router {
    not_found_handler (setStatusCode 404 >=> text "Api 404")
    pipe_through api

    forward "/someApi" someScopeOrController
}

(**

Here we have the `apiRouter` router which does not return a 404 page but a 404 text instead which is appropriate for an API. The router also uses a pipeline that is more appropriate for an API such as accepting JSON inputs instead of HTML as in the `browser` pipeline.

## Format Strings

You might be wondering how to make routes that accept a numerical ID. You can make multiple routes for each ID like so

```fsharp
get "/1" (getApplication 1)
get "/2" (getApplication 2)
get "/3" (getApplication 3)
...
```

But this is impracticle because there can be a large number of items or new items are constantly being created with new IDs. Instead the solution is to use format strings. Remember that in the [Adding Pages Guide](../tutorials/adding-pages.html), we used `getf "/%s" index2Action` to pass a string to page.

| Format Char | Type |
| ----------- | ---- |
| `%b` | `bool` |
| `%c` | `char` |
| `%s` | `string` |
| `%i` | `int` |
| `%d` | `int64` |
| `%f` | `float`/`double` |
| `%O` | `Guid` |

For a numerical ID, we want to pass an int which is `%i` in the list above, so you can replace the lines above with

```fsharp
getf "/%i" getApplication
```

Notice that `getf` is used instead of get. This is a separate version of get that handles `f`ormat characters.

    You can use format strings with "forward" too by using "forwardf"

## API Reference

Full API reference for `router` CE can be found [here](../reference/Saturn/saturn-router-routerbuilder.html)

*)
