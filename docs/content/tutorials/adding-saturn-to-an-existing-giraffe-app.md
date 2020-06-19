---
title: Adding Saturn to an existing Giraffe application
category: tutorial
menu_order: 2
---

# Adding Saturn to an existing Giraffe application

The previous chapter showed how to get started with a new application.

If you already have a working Giraffe webserver, you can gradually opt-in to Saturn.

For example, if your existing app looks like this:

```fsharp

type Customer = {
    Name : string
    Address : string
}

let customers =
    choose [
      GET >=> (json { Name = "Mr. Smith"; Address = "Santa Monika"})
      PUT >=> (bindJson<Customer> (fun customer -> printfn "Adding customer %A" customer; setStatusCode 200))
    ]


let webApp =
    choose [
        route "/"               >=> htmlFile "/pages/index.html"
        route "/api/customers"   >=> customers
    ]
```

and you need to add "vendor" functionality, you could implement it as a Saturn ``router`` while keeping everything else intact:


```fsharp
// the new Saturn router
let vendors = router {
    getf "/%i" (fun vendorId -> (json (readVendorFromDb vendorId)))
    post "/" (bindJson<Vendor> (fun customer -> addVendor vendor; setStatusCode 200))
}

let webApp =
    choose [
        route "/"                >=> htmlFile "/pages/index.html"
        route "/api/customers"   >=> customers
        // plug the new Saturn router into the Giraffe app
        route "/api/vendors"     >=> vendors
    ]
```

## Embedding Giraffe Handlers into Saturn

Of course the other way around also works.

For example, [Elmish.Bridge](https://github.com/Nhowka/Elmish.Bridge) does not provide a specialized implementation for Saturn. And it doesn't need to, because we can just use the Giraffe implementation!

```fsharp

open Elmish
open Elmish.Bridge

let elmishBridgeHandler : HttpHandler =
  Bridge.mkServer Shared.endpoint init update
  |> Bridge.run Giraffe.server

// our existing Saturn router
let router = router {

    // ...

    forward "" elmishBridgeHandler
}
```
