#r "../../_lib/Fornax.Core.dll"
#if !FORNAX
#load "../../loaders/contentloader.fsx"
#load "../../loaders/pageloader.fsx"
#load "../../loaders/globalloader.fsx"
#endif

open Html


let menu (ctx : SiteContents) =
  let shortcuts = ctx.GetValues<Pageloader.Shortcut> ()
  let references = ctx.GetValues<Pageloader.ApiReferences> ()

  let content = ctx.GetValues<Contentloader.Post> ()
  let siteTree = ctx.TryGetValue<Contentloader.FsTree>().Value
  let siteInfo = ctx.TryGetValue<Globalloader.SiteInfo>().Value
  let rootUrl = siteInfo.root_url

  let rec render root prefix (Contentloader.Node(lst)) =
    let lst =
      lst
      |> List.map (fun (name, tree) ->
        let link = root + "/" + name
        let post = content |> Seq.tryFind (fun c -> c.link = "content" + link)
        name, post, tree
      )
      |> List.filter (fun (_,s,_) -> s |> Option.map (fun s -> not s.hide_menu) |> Option.defaultValue true)
      |> List.sortBy (fun (_,s,_) -> s |> Option.map (fun s -> s.menu_order) |> Option.defaultValue 999)
    let mutable v = 0;
    [
      for (name, post, Contentloader.Node(tree)) in lst do
        v <- v + 1
        let prefix = if prefix = "" then sprintf "%d" v else sprintf "%s.%d" prefix v
        let link = root + "/" + name
        let title =
          post
          |> Option.map (fun c -> c.title)
          |> Option.defaultValue name
        if tree.IsEmpty then
          li [Class "dd-item"] [
            yield a [Href (rootUrl + link)] [ b [] [!! prefix]; !!title]
          ]
        else
          li [Class "dd-item parent"] [
            yield a [] [b [] [!! prefix]; !!title]
            yield ul [Class "child"] (render link prefix (Contentloader.Node(tree)) )
        ]
  ]

  let renderRefs =
    section [Id "shortcuts"] [
      h3 [] [!! "API References"]
      ul [] [
        for r in references ->
          li [] [
            a [Class "padding"; Href (rootUrl + r.link) ] [
              !! r.title
            ]
          ]
      ]
    ]

  let renderShortucuts =
    section [Id "shortcuts"] [
        h3 [] [!! "Shortucts"]
        ul [] [
          for s in shortcuts do
            yield
              li [] [
                a [Class "padding"; Href s.link ] [
                  i [Class s.icon] []
                  !! s.title
                ]
              ]
        ]
      ]

  let renderFooter =
    section [Id "footer"] [
      !! """<p>Built with <a href="https://github.com/ionide/Fornax">Fornax</a>, inspired by <a href="https://learn.netlify.com/en/">Hugo Learn</a></p>"""
    ]


  nav [Id "sidebar"] [
    div [Id "header-wrapper"] [
      div [Id "header"] [
        h2 [Id "logo"] [!! siteInfo.title]
      ]
      div [Class "searchbox"] [
        label [Custom("for", "search-by")] [i [Class "fas fa-search"] []]
        input [Custom ("data-search-input", ""); Id "search-by"; Type "search"; Placeholder "Search..."]
        span  [Custom ("data-search-clear", "")] [i [Class "fas fa-times"] []]
      ]
      script [Type "text/javascript"; Src (rootUrl + "/static/js/lunr.min.js")] []
      script [Type "text/javascript"; Src (rootUrl + "/static/js/auto-complete.js")] []
      script [Type "text/javascript";] [!! (sprintf "var baseurl ='%s'" rootUrl)]
      script [Type "text/javascript"; Src (rootUrl + "/static/js/search.js")] []
      script [Src (rootUrl + "/static/js/highlight.pack.js")] []
      script [] [!! "hljs.initHighlightingOnLoad();"]
    ]
    div [Class "highlightable"] [
      ul [Class "topics"] (render "" "" siteTree)
      renderRefs
      renderShortucuts
      renderFooter
    ]
  ]