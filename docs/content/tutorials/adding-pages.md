---
title: Adding Pages
category: tutorial
menu_order: 2
---

# Adding Pages

This guide uses the same project from the [how to start guide](how-to-start.md). Let's add two pages to it - one hello page and a page that can get your name from the URL.

## Creating the View

To begin, create a `Hello` folder inside the `src/SaturnSample` folder.

Inside the folder, create a new file called "HelloViews.fs". This file will contain the functions to create the page.

Insert the following into the file:

```fsharp
namespace Hello

open Giraffe.GiraffeViewEngine
open Saturn

module Views =
  let index =
    div [] [
        h2 [] [rawText "Hello from Saturn!"]
    ]
```

One of the dependencies required is [Giraffe View Engine](https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md#giraffe-view-engine). This will allow your project to define HTML within your function. The `index` function will result in the following HTML code:

```html
<div>
    <h2>Hello from Saturn!</h2>
</div>
```

## Creating the Controller

Create a `HelloController.fs` file inside the `Hello` folder.

The `index` function tells us what the HTML will be but we still need to tell Saturn to return it as an HTML page. We also need to tell Saturn where the page is.

Insert the following into the file:

```fsharp
namespace Hello

open Saturn
open Giraffe.ResponseWriters

module Controller =
    let indexAction =
        htmlView (Views.index)

    let helloView = router {
        get "/" indexAction
    }
```

The `indexAction` tells Saturn to create an HTML page using the `index` function inside "HelloViews.fs"

`helloView` lets Saturn know that the page is located at the root.

## Adding the 2 new files to the project

For the project to see the new files, modify SaturnSample.fsproj as below:

```xml
<ItemGroup>
    <Compile Include="Database.fs" />
    <Compile Include="Config.fs" />

    <Compile Include="Hello\HelloViews.fs" />
    <Compile Include="Hello\HelloController.fs" />
    ...
```

## Adding it to Router.fs


After setting up the route, you need to update the project with the new route.

In "Router.fs", add the following to the inside of the `browserRouter` function:


```fsharp
forward "/hello" Hello.Controller.helloView
```

This means that when we navigate to [http://localhost:8085/hello](http://localhost:8085/hello), the `helloView` function will determine what page to load there. Looking inside the `helloView` function, we said that `indexAction` is called at the root. In conclusion, the page will be located at [http://localhost:8085/hello/](http://localhost:8085/hello/). (Note the "/" at the end)

Now run the program and go to [http://localhost:8085/hello/](http://localhost:8085/hello/) and you will see a page saying "Hello from Saturn!"

## Sending a parameter to your page

What if you want the page to display your name?

One way to retrieve your name is to get it from the route. So when you go to [http://localhost:8085/hello/{yourname}](http://localhost:8085/hello/{yourname}) with {yourname} being your actual name, it will grab your name which can then be used to display on the page.

To begin, add a new view in your `HelloViews.fs`:

```fsharp
  let index2 (name : string) =
    div [] [
        h2 [] [rawText ("Hello " + name + "!")]
    ]
```

This function requires passing in the name to be displayed. The name will be retrieved from the route.

Add the following to the `HelloController.fs` file below the `helloView` handler:

```fsharp
let index2Action name=
    htmlView (Hello.Views.index2 name)
```

Now to set up the route. Add the following to the `HelloView` handler:

```fsharp
getf "/%s" index2Action
```

"%s" is a format string. This lets Saturn know to save whatever you type in that spot. Since we want to save a name, we want to save it as a string so we use `%s`.

There are other format strings for different types:

| Format String | Type |
| ----------- | ---- |
| `%b` | `bool` |
| `%c` | `char` |
| `%s` | `string` |
| `%i` | `int` |
| `%d` | `int64` |
| `%f` | `float`/`double` |
| `%O` | `Guid` |

Now run the program and go to [http://localhost:8085/hello/{yourname}](http://localhost:8085/hello/{yourname}) and replace `{yourname}` with your name to see a page that will greet you.
