namespace Saturn
open System.IO

module SiteMap =
    let mutable private isDebug = true //Somehow detect if Saturn is used by application compiled in debug or release

    type internal PathRegistrationEvent =
        | Path of path: string * verb: string
        | Forward of path: string * verb: string * key: obj
        | NotFound



    type HandlerMap () =
        let paths = ResizeArray<PathRegistrationEvent>()
        let mutable key : obj = null
        member val Version : int option = None with get, set

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

    let add hm = state.Add(hm)
    let generate () =
        let s = state |> Seq.last
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

