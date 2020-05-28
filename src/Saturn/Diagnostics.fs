namespace Saturn
open System
open System.IO

module SiteMap =
    let mutable internal isDebug = true //Somehow detect if Saturn is used by application compiled in debug or release

    type internal PathRegistrationEvent =
        | Path of path: string * verb: string
        | Forward of path: string * verb: string * key: obj
        | NotFound

    type PathEntry = {
        Route: string
        Verb: string
        Headers: Map<string, string>
    }


    type HandlerMap () =
        let paths = ResizeArray<PathRegistrationEvent>()
        let mutable key : obj = null
        member val Version : string option = None with get, set

        member __.AddPath p v = paths.Add(Path(p,v))

        member __.Forward p v k = paths.Add(Forward(p,v,k))

        member __.NotFound () = paths.Add(NotFound)

        member __.SetKey k = key <- k

        member __.GetKey () = key

        member internal __.GetPaths () = paths

        member this.CollectPaths prefix version (state : HandlerMap seq) =
            let ver =
                match this.Version with
                | None -> version
                | Some v -> Some v
            paths |> Seq.collect (function
                | Forward(p,v,k) ->
                    match state |> Seq.tryFind (fun s -> s.GetKey () = k) with
                    | Some res ->
                        res.CollectPaths (prefix + p) ver state
                    | None ->
                        ((prefix + p), v, ver)
                        |> Seq.singleton
                | Path(p,v) ->
                    ((prefix + p), v, ver)
                    |> Seq.singleton
                | NotFound ->
                    (prefix, "NotFoundHandler", ver)
                    |> Seq.singleton
            )
    let private state = ResizeArray<HandlerMap> ()

    let internal add hm = state.Add(hm)
    let internal generate () =
        try
          match state |> Seq.tryLast with
          | None -> ()
          | Some s ->
          let z = s.CollectPaths "" None state
          let typ = typeof<HandlerMap>
          let asm = typ.Assembly.Location
          let p = Path.Combine(Path.GetDirectoryName(asm), "site.map")
          let cnt =
              z
              |> Seq.map (fun (a,b,c) ->
                  let c = (c |> function Some v -> v.ToString() | None -> "")
                  sprintf "%s, %s, %s" a b c
              )
              |> String.concat "\n"
          File.WriteAllText(p, cnt)
        with
        | _ ->
          printfn "WARN - Couldn't write diagnostic `site.map` file"

    let getPaths () =
        match state |> Seq.tryLast with
        | None -> Seq.empty
        | Some s ->
            s.CollectPaths "" None state
            |> Seq.map (fun (a,b,c) -> {Route = a; Verb =b; Headers = Map.ofList [if c.IsSome then yield ("x-controller-version", c.Value)] })

    //TODO
    // open Giraffe.ResponseWriters
    // open Giraffe.GiraffeViewEngine
    // open Giraffe

    // let page : HttpHandler =
    //     fun next ctx ->
    //         let paths = getPaths ()
    //         let generateLink (route: string) =
    //             let hasB = route.Contains "%b"
    //             let hasC = route.Contains "%c"
    //             let hasS = route.Contains "%s"
    //             let hasI = route.Contains "%i"
    //             let hasD = route.Contains "%d"
    //             let hasF = route.Contains "%f"
    //             let hasO = route.Contains "%O"
    //             let hasU = route.Contains "%u"

    //             let link = route
    //             let info = ""
    //             let link, info = if hasB then link.Replace("%b", "true"), info + "`%b` replaced with `true`" else link, info
    //             let link, info = if hasC then link.Replace("%c", "x"), info + "`%c` replaced with `x`" else link, info
    //             let link, info = if hasS then link.Replace("%s", "sample"), info + "`%s` replaced with `sample`" else link, info
    //             let link, info = if hasI then link.Replace("%i", "123"), info + "`%i` replaced with `123`" else link, info
    //             let link, info = if hasD then link.Replace("%d", "123"), info + "`%d` replaced with `123`" else link, info
    //             let link, info = if hasF then link.Replace("%f", "123.123"), info + "`%f` replaced with `123.123`" else link, info
    //             let link, info = if hasU then link.Replace("%u", "123"), info + "`%u` replaced with `123`" else link, info
    //             let link, info = if hasO then link.Replace("%O", Guid.Empty.ToString()), info + "`%O` replaced with `" + Guid.Empty.ToString() + "`" else link, info

    //             let route = if info <> "" then route + " (" + info + ")" else route
    //             a [_href link] [rawText route]


    //         let index =
    //             html [_class "has-navbar-fixed-top"] [
    //                 head [] [
    //                     meta [_charset "utf-8"]
    //                     meta [_name "viewport"; _content "width=device-width, initial-scale=1" ]
    //                     title [] [encodedText "Routing diagnostic page"]
    //                     link [_rel "stylesheet"; _href "https://maxcdn.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css" ]
    //                     link [_rel "stylesheet"; _href "https://cdnjs.cloudflare.com/ajax/libs/bulma/0.6.1/css/bulma.min.css" ]
    //                 ]
    //                 body [] [
    //                     div [_class "container "] [
    //                         h2 [ _class "title"] [rawText "Routes found in application"]
    //                         table [_class "table is-hoverable is-fullwidth"] [
    //                             thead [] [
    //                                 tr [] [
    //                                     th [] [rawText "Route"]
    //                                     th [] [rawText "Verb"]
    //                                     th [] [rawText "Required headers"]
    //                                 ]
    //                             ]
    //                             tbody [] [
    //                                 for path in (paths |> Seq.where (fun p -> p.Verb <> "NotFoundHandler")) do
    //                                     yield tr [] [
    //                                         td [] [if path.Verb = "GET" then yield generateLink path.Route else yield rawText path.Route]
    //                                         td [] [rawText path.Verb]
    //                                         td [] (path.Headers |> Seq.map (fun (kv) -> p [] [code [] [rawText kv.Key]; rawText " -> "; rawText kv.Value ] ) |> Seq.toList)
    //                                     ]
    //                             ]
    //                         ]

    //                         h2 [ _class "title"] [rawText "Not Found handlers for following subroutes"]
    //                         table [_class "table is-hoverable is-fullwidth"] [
    //                             thead [] [
    //                                 tr [] [
    //                                     th [] [rawText "Route"]
    //                                     th [] [rawText "Required headers"]
    //                                 ]
    //                             ]
    //                             tbody [] [
    //                                 for path in (paths |> Seq.where (fun p -> p.Verb = "NotFoundHandler")) do
    //                                     yield tr [] [
    //                                         td [] [rawText path.Route ]
    //                                         td [] (path.Headers |> Seq.map (fun (kv) -> p [] [code [] [rawText kv.Key]; rawText " -> "; rawText kv.Value ] ) |> Seq.toList)
    //                                     ]
    //                             ]
    //                         ]
    //                     ]
    //                 ]
    //             ]
    //         ctx.WriteHtmlStringAsync (Giraffe.GiraffeViewEngine.renderHtmlDocument index)

