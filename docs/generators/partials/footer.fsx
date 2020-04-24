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
        script [Src (rootUrl + "/static/js/clipboard.min.js")] []
        script [Src (rootUrl + "/static/js/perfect-scrollbar.min.js")] []
        script [Src (rootUrl + "/static/js/perfect-scrollbar.jquery.min.js")] []
        script [Src (rootUrl + "/static/js/jquery.sticky.js")] []
        script [Src (rootUrl + "/static/js/featherlight.min.js")] []

        script [Src (rootUrl + "/static/js/modernizr.custom-3.6.0.js")] []
        script [Src (rootUrl + "/static/js/learn.js")] []
        script [Src (rootUrl + "/static/js/hugo-learn.js")] []
        link [Rel "stylesheet"; Href (rootUrl + "/static/mermaid/mermaid.css")]
        script [Src (rootUrl + "/static/mermaid/mermaid.js")] []
        script [] [!! "mermaid.initialize({ startOnLoad: true });"]
        script [Src "//cdnjs.cloudflare.com/ajax/libs/highlight.js/10.0.0/highlight.min.js"] []
        script [Src "https://cdnjs.cloudflare.com/ajax/libs/highlight.js/10.0.0/languages/fsharp.min.js"] []
        script [] [
          !! "hljs.initHighlightingOnLoad()"
        ]
    ]
