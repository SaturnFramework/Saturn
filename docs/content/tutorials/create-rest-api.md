---
title: Creating and Consuming REST API
category: tutorial
menu_order: 4
---

# Creating REST API

Starting from the [how to start guide](how-to-start.html), we will create a Book API to create, read, update, and delete. 

First we need set up the application to handle api routes. Uncomment `forward "/api" apiRouter` line in `appRouter` inside Router.fs. Then, uncomment the `api` pipeline and `apiRouter` in the same file. Finally, change `forward "/someApi" someScopeOrController` to  `forward "/books" Books.Controller.apiRouter` inside `apiRouter`. 

We also need to clean up the template. Remove the `error_handler (text "Api 404")` line inside Router.fs. Remove `open FSharp.Control.Tasks.ContextInsensitive` from BookRepository.fs and BookController.fs file inside the Books folder. Add `open Giraffe` to BookController.fs. Finally, replace `open Giraffe.GiraffeViewEngine` with `open Giraffe.ViewEngine` inside BooksViews.fs.

## Configure Endpoint Routing

We will be using [Endpoint Routing](https://saturnframework.org/explanations/endpoint-routing.html) for the application. To do this, add `open Saturn.Endpoint` to the Router.fs and BooksController.fs files. Then inside Program.fs, replace `use_router Router.appRouter` with `use_endpoint_router Router.appRouter`.

## API Router

We need to create router for our API endpoints. Create the following `apiRouter` at the bottom inside BooksController.fs to handle the standard GET, POST, PUT, DELETE.

```fsharp
let apiRouter =
    router {
        get "/" getAction
        getf "/%s" (fun id -> getByIdAction id)
        post "/" (bindJson<Book> (fun book -> postAction book))
        putf "/%s" (fun id -> (bindJson<Book> (fun book -> putAction id book)))
        deletef "/%s" (fun id -> deleteApiAction id)
    }
```

This will be the base of our Books API endpoints at `"http://localhost:8085/api/books/"`.

This router will automatically pass `HttpFunc` and `HttpContext` to our action functions as parameters. Now, let's create our action functions.

We don't need an id to return a list of all books so the path for our GET request is `"http://localhost:8085/api/books/"` thanks to `forward "/books" Books.Controller.apiRouter` inside our Router.fs file.

To get a specific book by id, we will need to get our id from the URL. For instance, to get a book with an id of 2, we would make a GET request to `"http://localhost:8085/api/books/2"`. For that, we will use `getf` and the format string `"/%s"` to read the string from the URL and pass it on to our function. You can also write this as `getf "/%s" getByIdAction`. Refer to the table below to modify the format string to your need (e.g. use `"/%i"` if your id is an int).

For POST, we need to parse the object passed in with the request. Generally, this is a JSON object so `bindJson` is used. To test, send a POST request to `"http://localhost:8085/api/books/"` with the object inside the body as JSON.

For PUT, we need both the id and the object so we will have both `putf` and `bindJson` to get the values to pass to `putAction`.

For DELETE, the id is needed so we use `deletef`.

| Format String | Type |
| ----------- | ---- |
| `%b` | `bool` |
| `%c` | `char` |
| `%s` | `string` |
| `%i` | `int` |
| `%d` | `int64` |
| `%f` | `float`/`double` |
| `%O` | `Guid` |

## GET

Now we create functions to handle our API request. These functions go before the `apiRouter` function. First is a function to handle GET HTTP request, create the `getAction` function below at the bottom of the BooksController.fs file. We will use the existing database functions to retrieve data.

```fsharp
let getAction (next: HttpFunc) (ctx: HttpContext) =
    task {
        let cnf = Controller.getConfig ctx
        let! result = Database.getAll cnf.connectionString

        match result with
        | Ok result -> return! Response.ok ctx (List.ofSeq result)
        | Error ex -> return! Response.internalError ctx ex.Message
    }
```

This GET request will return a `200 OK` with the list of books inside our database. If there is a problem getting data from the database, return a `500 Internal Error`. You can also use `return! (Successful.OK (List.ofSeq result) next ctx)` and `return! (ServerErrors.INTERNAL_ERROR ex.Message next ctx)` for OK and Internal Error.

All our REST action methods will require a `HttpFunc` and `HttpContext` object passed in. We use the `HttpContext` object to retrieve our connection string and in returning the response code. The `HttpFunc` object is not used but is required for our router function.

We also need a GET action to retrieve an item by id. Create a new function with the id in the parameter. We will return a `404 Not Found` error if no book exists with that id.

```fsharp
let getByIdAction id (next: HttpFunc) (ctx: HttpContext) =
    task {
        let cnf = Controller.getConfig ctx
        let! result = Database.getById cnf.connectionString id

        match result with
        | Ok (Some result) -> return! Response.ok ctx result
        | Ok None -> return! Response.notFound ctx ""
        | Error ex -> return! Response.internalError ctx ex.Message
    }
```

>It is important to put any neccessary parameters before `HttpFunc` and `HttpContext`.


## POST

For POST, we need a `Book` object passed in. We will be using the existing `validate` function to check that the object is valid. If the object is invalid, return `400 Bad Request`. You can return an message stating why the object is invalid but for the example, we return an empty string.

```fsharp
let postAction book (next: HttpFunc) (ctx: HttpContext) =
    task {
        let validateResult = Validation.validate book

        if validateResult.IsEmpty then
            let cnf = Controller.getConfig ctx
            let! result = Database.insert cnf.connectionString book

            match result with
            | Ok _ -> return! Response.ok ctx book
            | Error ex -> return! Response.internalError ctx ex.Message
        else
            return! Response.badRequest ctx ""
    }
```

## PUT

To update a book, we need the id and a book object with updated data. First, we check that there is a book with the same id. Then we check that the data is valid. Then we update the book.

```fsharp
let putAction id (book: Book) (next: HttpFunc) (ctx: HttpContext) =
    task {
        match id = book.id with
        | true ->
            let cnf = Controller.getConfig ctx
            let! result = Database.getById cnf.connectionString id

            match result with
            | Ok (Some result) ->
                let validateResult = Validation.validate result

                if validateResult.IsEmpty then
                    let cnf = Controller.getConfig ctx
                    let! result = Database.update cnf.connectionString book

                    match result with
                    | Ok _ -> return! Response.ok ctx result
                    | Error ex -> return! Response.internalError ctx ex.Message
                else
                    return! Response.badRequest ctx validateResult.Values
            | Ok None -> return! Response.notFound ctx ""
            | Error ex -> return! Response.notFound ctx ex.Message
        | false -> return! Response.badRequest ctx ""
    }
```

## DELETE

Deletion requires the id of the object to be deleted. We need a check to see that the book with the id exist.

```fsharp
let deleteApiAction id (next: HttpFunc) (ctx: HttpContext) =
    task {
        let cnf = Controller.getConfig ctx
        let! result = Database.getById cnf.connectionString id

        match result with
        | Ok (Some book) ->
            let! result = Database.delete cnf.connectionString id

            match result with
            | Ok _ -> return! Response.ok ctx book
            | Error ex -> return! Response.internalError ctx ()
        | Ok None -> return! Response.notFound ctx ()
        | Error ex -> return! Response.internalError ctx ()
    }
```

>Since there is already a `deleteAction` if you are using the how to start guide. I named this function `deleteApiAction`.

# Consuming REST API

This project already have functions to retrieve data from the database and return them to the view. We will modify the functions to use the API instead. Start with adding the `System.Net.Http.Json` nuget package to the `SaturnSample` project. Then add the following declarations to BooksController.fs

```fsharp
open System.Net
open System.Net.Http
open System.Net.Http.Json
open System.Text
open System.Text.Json
```

## GET

We will change the `indexAction` function to call our GET API. If we get a `200 OK` response then we will get a list of books from the JSON result and render the page. Otherwise, we will show the Not Found page.

```fsharp
let indexAction (ctx: HttpContext) =
    task {
        use client = new HttpClient(BaseAddress=Uri("http://localhost:8085/api/books/"))
        let! responseTask = client.GetAsync("")
        match responseTask.StatusCode with
        | HttpStatusCode.OK ->
            let! books = responseTask.Content.ReadFromJsonAsync<List<Book>>()
            return! Controller.renderHtml ctx (Views.index ctx (List.ofSeq books))
        | _ ->
            return! Controller.renderHtml ctx (NotFound.layout)
    }
```

`showAction` will be similar but the API expects the id from the URL. Inside the `GetAsync` function, pass in the id. This send a GET request to `"http://localhost:8085/api/books/{id}"` for the specified id.

```fsharp
let showAction (ctx: HttpContext) (id: string) =
    task {
        use client = new HttpClient(BaseAddress=Uri("http://localhost:8085/api/books/"))
        let! responseTask = client.GetAsync(id) // Add id to GET request address.
        match responseTask.StatusCode with
        | HttpStatusCode.OK ->
            let! book = responseTask.Content.ReadFromJsonAsync<Book>()
            return! Controller.renderHtml ctx (Views.show ctx book)
        | _ ->
            return! Controller.renderHtml ctx (NotFound.layout)
    }
```

Since the `editAction` only get the Book object to display the edit form. It will also send a GET Request.

```fsharp
let editAction (ctx: HttpContext) (id: string) =
    task {
        use client = new HttpClient(BaseAddress=Uri("http://localhost:8085/api/books/"))
        let! responseTask = client.GetAsync(id)
        match responseTask.StatusCode with
        | HttpStatusCode.OK ->
            let! book = responseTask.Content.ReadFromJsonAsync<Book>()
            return! Controller.renderHtml ctx (Views.edit ctx book Map.empty)
        | _ ->
            return! Controller.renderHtml ctx (NotFound.layout)
    }
```

## POST

To create a new Book, we need to send the Book object from our model as a JSON object. First, check that the object is valid then convert it into JSON. Pass this JSON object inside our POST Request. 

```fsharp
let createAction (ctx: HttpContext) =
    task {
        let! input = Controller.getModel<Book> ctx // Grab object from the Model
        let validateResult = Validation.validate input
        if validateResult.IsEmpty then
            let json = JsonSerializer.Serialize(input)
            let content = new StringContent(json, Encoding.UTF8, "application/json") // Convert object into json
            use client = new HttpClient(BaseAddress=Uri("http://localhost:8085/api/books/"))
            let! responseTask = client.PostAsync("", content) // Include json object inside request
            match responseTask.StatusCode with
            | HttpStatusCode.OK ->
                return! Controller.redirect ctx (Links.index ctx)
            | _ ->
                return! Controller.renderHtml ctx (NotFound.layout)
        else
            return! Controller.renderHtml ctx (Views.add ctx (Some input) validateResult)
    }
```

## PUT

Update is similar to create but we need an id.

```fsharp
let updateAction (ctx: HttpContext) (id: string) =
    task {
        let! input = Controller.getModel<Book> ctx
        let validateResult = Validation.validate input
        if validateResult.IsEmpty then
            use client = new HttpClient(BaseAddress=Uri("http://localhost:8085/api/books/"))
            let json = JsonSerializer.Serialize(input)
            let content = new StringContent(json, Encoding.UTF8, "application/json")
            let! responseTask = client.PutAsync(id, content) // Add id to the address and include json object
            match responseTask.StatusCode with
            | HttpStatusCode.OK ->
                return! Controller.redirect ctx (Links.index ctx)
            | _ ->
                return! Controller.renderHtml ctx (NotFound.layout)
        else
            return! Controller.renderHtml ctx (Views.edit ctx input validateResult)
    }
```

## DELETE

Send a DELETE request with the id in the URL address.

```fsharp
let deleteAction (ctx: HttpContext) (id: string) =
    task {
        use client = new HttpClient(BaseAddress=Uri("http://localhost:8085/api/books/"))
        let! responseTask = client.DeleteAsync(id)
        match responseTask.StatusCode with
        | HttpStatusCode.OK ->
            return! Controller.redirect ctx (Links.index ctx)
        | _ ->
            return! Controller.renderHtml ctx (NotFound.layout)
    }
```

> For simplicity, we render the Not Found page when the request does not return with a `200 OK` response. We will want to handle the different error codes separately in a production environment.
