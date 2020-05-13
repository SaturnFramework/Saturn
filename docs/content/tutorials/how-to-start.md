---
title: Getting started
category: tutorial
menu_order: 1
---

# How to start in 60 seconds

Requirements:

* `dotnet` SDK 3.1 [https://dotnet.microsoft.com/download/dotnet-core/3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1)

## Template

The easiest way to get started is to use the provided template:

1. Install the `dotnet` template with `dotnet new -i Saturn.Template`
2. Create a new folder and move into it - `mkdir SaturnSample && cd SaturnSample`
3. Create a new Saturn application - `dotnet new saturn -lang F#`
4. Install all necessary dev tools - `dotnet tool restore`
6. Create a new controller with `dotnet saturn gen Book Books id:string title:string author:string`
7. Run migrations that will create the database and Books table (as for now, the generator is using only SQLite DB) - `dotnet saturn migration`
8. Open the folder in your favourite editor (VSCode) and insert the line (`forward "/books" Books.Controller.resource`) into `browserRouter` in `Router.fs` file
9. Start the application by running `dotnet fake build -t run` from the root of the solution. This will start the application in watch mode (automatic recompilation on changes) and open your browser on [http://localhost:8085](http://localhost:8085) which should display the index page.
10. Go to [http://localhost:8085/books](http://localhost:8085/books) to see the generated view. All buttons should be working; you can add new entries, and remove or edit old ones.


## Hello World

If you want to start from scratch with a minimal Saturn webserver:

1. Create a new F# Project (for example with `dotnet new console -lang F#`)
2. Add the `Saturn` NuGet Package

```f#
open Saturn
open Giraffe

let app = application {
    use_router (text "Hello World from Saturn")
}

run app
```

If you compile and run this application, it will unconditionally return the text regardless of the path.

From here on out you can add [routers](../explanations/routing.md), [controllers](../explanations/controller.md) and [views](../explanations/view.md).
