#r "../../_lib/Fornax.Core.dll"
#if !FORNAX
#load "../../loaders/contentloader.fsx"
#load "../../loaders/pageloader.fsx"
#load "../../loaders/globalloader.fsx"
#endif

open Html



let footer (ctx : SiteContents)  =
    let siteInfo = ctx.TryGetValue<Globalloader.SiteInfo>().Value
    let rootUrl = siteInfo.root_url

    [
        div [Custom("style", "left: -1000px; overflow: scroll; position: absolute; top: -1000px; border: none; box-sizing: content-box; height: 200px; margin: 0px; padding: 0px; width: 200px;")] [
            div [Custom("style", "border: none; box-sizing: content-box; height: 200px; margin: 0px; padding: 0px; width: 200px;")] []
        ]

        script [Src (rootUrl.subRoute "/static/js/modernizr.custom-3.6.0.js")] []
        script [Src (rootUrl.subRoute "/static/js/scripts.js")] []

        script [Src "//cdnjs.cloudflare.com/ajax/libs/jquery.sticky/1.0.4/jquery.sticky.min.js"] []
        script [Src "//cdnjs.cloudflare.com/ajax/libs/clipboard.js/2.0.6/clipboard.min.js"] []
        script [Src "//cdnjs.cloudflare.com/ajax/libs/jquery.perfect-scrollbar/1.5.0/perfect-scrollbar.min.js"] []
        script [Src "//cdnjs.cloudflare.com/ajax/libs/featherlight/1.7.13/featherlight.min.js"] []
        script [Src "//cdnjs.cloudflare.com/ajax/libs/mermaid/8.5.2/mermaid.min.js"] []
        script [Src "//cdnjs.cloudflare.com/ajax/libs/highlight.js/10.0.0/highlight.min.js"] []
        script [Src "//cdnjs.cloudflare.com/ajax/libs/highlight.js/10.0.0/languages/fsharp.min.js"] []
        script [Src (rootUrl.subRoute "/static/js/tips.js")] []

        script [] [
          !! "hljs.initHighlightingOnLoad()"
        ]
        script [] [!! "mermaid.initialize({ startOnLoad: true });"]
    ]
