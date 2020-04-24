#r "../../_lib/Fornax.Core.dll"
#if !FORNAX
#load "../../loaders/contentloader.fsx"
#load "../../loaders/pageloader.fsx"
#load "../../loaders/globalloader.fsx"
#endif

open Html

let header (ctx : SiteContents) page =
    let siteInfo = ctx.TryGetValue<Globalloader.SiteInfo>().Value
    let rootUrl = siteInfo.root_url

    head [] [
        meta [CharSet "utf-8"]
        meta [Name "viewport"; Content "width=device-width, initial-scale=1"]
        title [] [!! (siteInfo.title + " | " + page)]
        link [Rel "icon"; Type "image/png"; Sizes "32x32"; Href (rootUrl + "/static/images/favicon.png")]
        link [Rel "stylesheet"; Href (rootUrl + "/static/css/nucleus.css")]
        link [Rel "stylesheet"; Href (rootUrl + "/static/css/fontawesome-all.min.css")]
        link [Rel "stylesheet"; Href (rootUrl + "/static/css/hybrid.css")]
        link [Rel "stylesheet"; Href (rootUrl + "/static/css/featherlight.min.css")]
        link [Rel "stylesheet"; Href (rootUrl + "/static/css/perfect-scrollbar.min.css")]
        link [Rel "stylesheet"; Href (rootUrl + "/static/css/auto-complete.css")]
        link [Rel "stylesheet"; Href (rootUrl + "/static/css/atom-one-dark-reasonable.css")]
        link [Rel "stylesheet"; Href (rootUrl + "/static/css/theme.css")]
        link [Rel "stylesheet"; Href "//cdnjs.cloudflare.com/ajax/libs/highlight.js/10.0.0/styles/atom-one-dark.min.css"]
        if siteInfo.theme_variant.IsSome then
            link [Rel "stylesheet"; Href (rootUrl + (sprintf "/static/css/theme-%s.css" siteInfo.theme_variant.Value))]
        script [Src (rootUrl + "/static/js/jquery-3.3.1.min.js")] []
    ]
