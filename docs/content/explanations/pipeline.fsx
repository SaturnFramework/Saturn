(**
---
title: Pipeline
category: explanation
menu_order: 4

---
*)
(*** hide ***)
#I "../../../temp/"
#I "../../../packages/docsasp/Microsoft.AspNetCore.app.ref/ref/net5.0"
#r "Saturn.dll"
#r "Giraffe.dll"
#r "Microsoft.AspNetCore.Http.Abstractions.dll"

(**
# Pipelines

Pipeline is a computation expression used to combine `HttpHandlers` in a declarative manner.

The result of the computation expression is a standard Giraffe `HttpHandler` which means that it's easily composable with other parts of the Giraffe ecosystem.

**Example:**

*)

open Saturn

let headerPipe = pipeline {
    set_header "myCustomHeader" "abcd"
    set_header "myCustomHeader2" "zxcv"
}

let endpointPipe = pipeline {
    plug fetchSession
    plug head
    plug requestId
}


(**
## API Reference

Full API reference for `pipeline` CE can be found [here](../reference/Saturn/saturn-pipeline-pipelinebuilder.html)

Full API reference for `PipelineHelpers` module containing useful helpers can be found [here](../reference/Saturn/saturn-pipelinehelpers.html)

You can also use in pipelines (using `plug`) any `HttpHandler` defined in Giraffe - documentation can be found [here](https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md)
*)
