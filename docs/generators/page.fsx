#r "../_lib/Fornax.Core.dll"
#load "partials/layout.fsx"

open Html


let generate' (ctx : SiteContents) (page: string) =
    let posts =
        ctx.TryGetValues<Contentloader.Post> ()
        |> Option.defaultValue Seq.empty
    let post = posts |> Seq.find (fun n -> "content/" + n.file = page)
    Layout.layout ctx [ !! post.content ] post.title

let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    generate' ctx page
    |> Layout.render ctx
