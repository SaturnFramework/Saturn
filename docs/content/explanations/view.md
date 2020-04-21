---
title: View Engine
category: explanation
menu_order: 6
---

# View Engine

> This post has been originally part of the [Giraffe documentation](https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md#giraffe-view-engine)

Saturn is built on top of Giraffe and can use any helpers it provides. This is a case for suggested view engine that you can use in Saturn - Giraffe has its own functional view engine which can be used to build rich UIs for web applications. The single biggest and best contrast to other view engines (e.g. Razor, Liquid, etc.) is that the Giraffe View Engine is entirely functional written in normal (and compiled) F# code.

This means that the Giraffe View Engine is by definition one of the most feature rich view engines available, requires no disk IO to load a view and views are automatically compiled at build time.

The Giraffe View Engine uses traditional functions and F# record types to generate rich HTML/XML views.

### HTML Elements and Attributes

HTML elements and attributes are defined as F# objects:

```fsharp
let indexView =
    html [] [
        head [] [
            title [] [ str "Giraffe Sample" ]
        ]
        body [] [
            h1 [] [ str "I |> F#" ]
            p [ _class "some-css-class"; _id "someId" ] [
                str "Hello World"
            ]
        ]
    ]
```

A HTML element can either be a `ParentNode`, a `VoidElement` or a `Text` element.

For example the `<html>` or `<div>` tags are typical `ParentNode` elements. They can hold an `XmlAttribute list` and a second `XmlElement list` for their child elements:

```fsharp
let someHtml = div [] []
```

All `ParentNode` elements accept these two parameters:

```fsharp
let someHtml =
    div [ _id "someId"; _class "css-class" ] [
        a [ _href "https://example.org" ] [ str "Some text..." ]
    ]
```

Most HTML tags are `ParentNode` elements, however there is a few HTML tags which cannot hold any child elements, such as `<br>`, `<hr>` or `<meta>` tags. These are represented as `VoidElement` objects and only accept the `XmlAttribute list` as single parameter:

```fsharp
let someHtml =
    div [] [
        br []
        hr [ _class "css-class-for-hr" ]
        p [] [ str "bla blah" ]
    ]
```

Attributes are further classified into two different cases. First and most commonly there are `KeyValue` attributes:

```fsharp
a [
    _href "http://url.com"
    _target "_blank"
    _class "class1" ] [ str "Click here" ]
```

As the name suggests, they have a key, such as `class` and a value such as the name of a CSS class.

The second category of attributes are `Boolean` flags. There are not many but some HTML attributes which do not require any value (e.g. `async` or `defer` in script tags). The presence of such an attribute means that the feature is turned on, otherwise it is turned off:

```fsharp
script [ _src "some.js"; _async ] []
```

There's also a wealth of [accessibility attributes](https://www.w3.org/TR/html-aria/) available under the `Giraffe.GiraffeViewEngine.Accessibility` module (needs to be explicitly opened).

### Text Content

Naturally the most frequent content in any HTML document is pure text:

```html
<div>
    <h1>This is text content</h1>
    <p>This is even more text content!</p>
</div>
```

The Giraffe View Engine lets one create pure text content as a `Text` element. A `Text` element can either be generated via the `rawText` or `encodedText` (or the short alias `str`) functions:

```fsharp
let someHtml =
    div [] [
        p [] [ rawText "<div>Hello World</div>" ]
        p [] [ encodedText "<div>Hello World</div>" ]
    ]
```

The `rawText` function will create an object of type `XmlNode` where the content will be rendered in its original form and the `encodedText`/`str` function will output a string where the content has been HTML encoded.

In this example the first `p` element will literally output the string as it is (`<div>Hello World</div>`) while the second `p` element will output the value as HTML encoded string `&lt;div&gt;Hello World&lt;/div&gt;`.

Please be aware that the the usage of `rawText` is mainly designed for edge cases where someone would purposefully want to inject HTML (or JavaScript) code into a rendered view. If not used carefully this could potentially lead to serious security vulnerabilities and therefore should be used only when explicitly required.

Most cases and particularly any user provided content should always be output via the `encodedText`/`str` function.

### Javascript event handlers

It is possible to add JavaScript event handlers to HTML elements using the Giraffe View Engine.  These event handlers (all prefixed with names starting with `_on`, for example `_onclick`, `_onmouseover`) can either execute inline JavaScript code or can invoke functions that are part of the `window` scope.

This example illustrates how inline JavaScript could be used to log to the console when a button is clicked:

```fsharp
let inlineJSButton =
    button [_id "inline-js"
            _onclick "console.log(\"Hello from the 'inline-js' button!\");"] [str "Say Hello" ]
```

There are some caveats with this approach, namely that
* it is not very scalable to write JavaScript inline in this manner, and more pressing
* the Giraffe View Engine HTML-encodes the text provided to the `_onX` attributes.

To get around this, you can write dedicated scripts in your HTML and reference the functions from your event handlers:

```fsharp
let page =
    div [] [
        script [_type "application/javascript"] [
            rawText """
            window.greet = function () {
                console.log("ping from the greet method");
            }
            """
        ]
        button [_id "script-tag-js"
                _onclick "greet();"] [str "Say Hello"]
    ]
```

Here it's important to note that we've included the text of our script using the `rawText` tag.  This ensures that our text is not encoded by Giraffe so that it remains as we have written it.

However, writing large quantities of JavaScript in this manner can be difficult, because you don't have access to the large ecosystem of javascript editor tooling.  In this case you should write your functions in another script and use a `script` tag element to reference your script, then add the desired function to your HTML element's event handler.

Say you had a JavaScript file named `greet.js` and had configured Giraffe to serve that script from the WebRoot. Let us also say that the content of that script was:

```javascript
function greet() {
    console.log("Hello from the greet function of greet.js!");
}
```

Then, you could reference that javascript via a script element, and use `greet` in your event handler like so:

```fsharp
let page =
    html [] [
        head [] [
            script [_type "application/javascript"
                    _src "/greet.js"] [] // include our `greet.js` function dynamically
        ]
        body [] [
            button [_id "greet-btn"
                    _onclick "greet()"] [] // use the `greet()` function from `greet.js` to say hello
        ]
    ]
```

In this way, you can write `greet.js` with all of your expected tooling, and still hook up the event handlers all in one place in Giraffe.

### Naming Convention

The Giraffe View Engine has a naming convention which lets you easily determine the correct function name without having to know anything about the view engine's implementation.

All HTML tags are defined as `XmlNode` elements under the exact same name as they are named in HTML. For example the `<html>` tag would be `html [] []`, an `<a>` tag would be `a [] []` and a `<span>` or `<canvas>` would be the `span [] []` or `canvas [] []` function.

HTML attributes follow the same naming convention except that attributes have an underscore prepended. For example the `class` attribute would be `_class` and the `src` attribute would be `_src` in Giraffe.

The underscore does not only help to distinguish an attribute from an element, but also avoid a naming conflict between tags and attributes of the same name (e.g. `<form>` vs. `<input form="form1">`).

If a HTML attribute has a hyphen in the name (e.g. `accept-charset`) then the equivalent Giraffe attribute would be written in camel case notion (e.g. `acceptCharset`).

*Should you find a HTML tag or attribute missing in the Giraffe View Engine then you can either [create it yourself](#custom-elements-and-attributes) or send a [pull request on GitHub](https://github.com/giraffe-fsharp/Giraffe/pulls).*

### View Engine Best Practices

Due to the huge amount of available HTML tags and their fairly generic (and short) names (e.g. `<form>`, `<option>`, `<select>`, etc.) there is a significant danger of accidentally overriding a function of the same name in an application's codebase. For that reason the Giraffe View Engine becomes only available after opening the `GiraffeViewEngine` module.

As a measure of good practice it is recommended to create all views in a separate module:

```fsharp
module MyWebApplication

module Views =
    open Giraffe.GiraffeViewEngine

    let index =
        html [] [
            head [] [
                title [] [ str "Giraffe Sample" ]
            ]
            body [] [
                h1 [] [ str "I |> F#" ]
                p [ _class "some-css-class"; _id "someId" ] [
                    str "Hello World"
                ]
            ]
        ]

    let other = //...
```

This ensures that the opening of the `GiraffeViewEngine` is only contained in a small context of an application's codebase and therefore less of a threat to accidental overrides. In the above example views can always be accessed through the `Views` sub module (e.g. `Views.index`).

### Custom Elements and Attributes

Adding new elements or attributes is normally as simple as a single line of code:

```fsharp
open Giraffe.GiraffeViewEngine

// If there was a new <foo></foo> HTML element:
let foo = tag "foo"

// If <foo> is an element which cannot hold any content then create it as voidTag:
let foo = voidTag "foo"

// If <foo> has a new attribute called bar then create a new bar attribute:
let _bar = attr "bar"

// if the bar attribute is a boolean flag:
let _bar = flag "bar"
```

Alternatively you can also create new elements and attributes from inside another element:

```fsharp
let someHtml =
    div [] [
        tag "foo" [ attr "bar" "blah" ] [
            voidTag "otherFoo" [ flag "flag1" ]
        ]
    ]
```

### Rendering Views

Rendering views in Giraffe is done through one of the following functions:

- `renderHtmlDocument`
- `renderHtmlNodes`
- `renderHtmlNode`
- `renderXmlNodes`
- `renderXmlNode`

The Giraffe View Engine cannot only be used to render HTML views, but also for any other XML based content such as `<svg>` images or other arbitrary XML based data.

The `renderHtmlDocument` function takes a single `XmlNode` as input parameter and renders a HTML page with a `DOCTYPE` declaration. This function should be used for rendering a complete HTML document. The `WriteHtmlViewAsync` extension method and the `htmlView` http handler both use the `renderHtmlDocument` function under the covers.

The `renderHtmlNodes` function takes an `XmlNode list` as input parameter and will output a single HTML string containing all the rendered HTML code. The `renderHtmlNode` function renders a single `XmlNode` element into a valid HTML string. Both, the `renderHtmlNodes` and `renderHtmlNode` function are useful for use cases where a HTML snippet needs to be created without a `DOCTYPE` declaration (e.g. templated emails, etc.).

The `renderXmlNodes` and `renderXmlNode` function are identical to `renderHtmlNodes` and `renderHtmlNode`, except that they will render void elements differently:

```fsharp
let someTag = voidTag "foo"
let someContent = someTag []

// Void tag will be rendered to valid HTML: <foo>
let output1 = renderHtmlNode someContent

// Void tag will be rendered to valid XML: <foo />
let output2 = renderXmlNode someContent
```

All `GiraffeViewEngine` http handlers are using a thread static `StringBuilderPool` to avoid the creation of large `StringBuilder` objects for each render call and dynamically grow/shrink that pool based on the application's needs. However if the application is running into any memory issues then this performance feature can be disabled by setting `StringBuilderPool.IsEnabled <- false`.

Additionally with Giraffe 3.0.0 or higher there is a new module called `ViewBuilder` under the `Giraffe.GiraffeViewEngine` namespace. This module exposes additional view rendering functions which compile a view into a `StringBuilder` object instead of returning a single `string`:

- `ViewBuilder.buildHtmlDocument`
- `ViewBuilder.buildHtmlNodes`
- `ViewBuilder.buildHtmlNode`
- `ViewBuilder.buildXmlNodes`
- `ViewBuilder.buildXmlNode`

The `ViewBuilder.build[...]` functions can be useful if there is additional string processing required before/after composing a view by the `GiraffeViewEngine` (e.g. embedding HTML snippets in an email template, etc.). These functions also serve as the lower level building blocks of the equivalent `render[...]` functions.

Example usage:

```fsharp
open System.Text
open Giraffe.GiraffeViewEngine

let someHtml =
    div [] [
        tag "foo" [ attr "bar" "blah" ] [
            voidTag "otherFoo" [ flag "flag1" ]
        ]
    ]

let sb = new StringBuilder()

// Perform actions on the `sb` object...
sb.AppendLine "This is a HTML snippet inside a markdown string:"
  .AppendLine ""
  .AppendLine "```html" |> ignore

let sb' = ViewBuilder.buildHtmlNode sb someHtml

// Perform more actions on the `sb` object...
sb'.AppendLine "```" |> ignore

let markdownOutput = sb'.ToString()
```

### Common View Engine Features

The Giraffe View Engine doesn't have any specially built functions for commonly known features such as master pages or partial views, mainly because the nature of the view engine itself doesn't require it in most cases.

#### Master Pages

Creating a master page is a simple matter of piping two functions together:

```fsharp
module Views =
    open Giraffe.GiraffeViewEngine

    let master (pageTitle : string) (content: XmlNode list) =
        html [] [
            head [] [
                title [] [ str pageTitle ]
            ]
            body [] content
        ]

    let index =
        let pageTitle = "Giraffe Sample"
        [
            h1 [] [ str pageTitle ]
            p [] [ str "Hello world!" ]
        ] |> master pageTitle
```

... or even have multiple nested master pages:

```fsharp
module Views =
    open Giraffe.GiraffeViewEngine

    let master1 (pageTitle : string) (content: XmlNode list) =
        html [] [
            head [] [
                title [] [ str pageTitle ]
            ]
            body [] content
        ]

    let master2 (content: XmlNode list) =
        [
            main [] content
            footer [] [
                p [] [
                    str "Copyright ..."
                ]
            ]
        ]

    let index =
        let pageTitle = "Giraffe Sample"
        [
            h1 [] [ str pageTitle ]
            p [] [ str "Hello world!" ]
        ] |> master2 |> master1 pageTitle
```

#### Partial Views

A partial view is nothing more than one function or object being called from within another function:

```fsharp
module Views =
    open Giraffe.GiraffeViewEngine

    let partial =
        footer [] [
            p [] [
                str "Copyright..."
            ]
        ]

    let master (pageTitle : string) (content: XmlNode list) =
        html [] [
            head [] [
                title [] [ str pageTitle ]
            ]
            body [] content
            partial
        ]

    let index =
        let pageTitle = "Giraffe Sample"
        [
            h1 [] [ str pageTitle ]
            p [] [ str "Hello world!" ]
        ] |> master pageTitle
```

#### Working with Models

A view which accepts a model is basically a function with an additional parameter:

```fsharp
module Views =
    open Giraffe.GiraffeViewEngine

    let partial =
        footer [] [
            p [] [
                str "Copyright..."
            ]
        ]

    let master (pageTitle : string) (content: XmlNode list) =
        html [] [
            head [] [
                title [] [ str pageTitle ]
            ]
            body [] content
            partial
        ]

    let index (model : IndexViewModel) =
        [
            h1 [] [ str model.PageTitle ]
            p [] [ str model.WelcomeText ]
        ] |> master model.PageTitle
```

#### If Statements, Loops, etc.

Things like if statements, loops and other normal F# language constructs work just as expected:

```fsharp
let partial (books : Book list) =
    ul [] [
        yield!
            books
            |> List.map (fun b -> li [] [ str book.Title ])
    ]
```

Overall the Giraffe View Engine is extremely flexible and feature rich by nature based on the fact that it is generated via normal compiled F# code.