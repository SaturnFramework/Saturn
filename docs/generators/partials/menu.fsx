#r "../../_lib/Fornax.Core.dll"
// #if !FORNAX
#load "../../loaders/apirefloader.fsx"
#load "../../loaders/contentloader.fsx"
#load "../../loaders/pageloader.fsx"
#load "../../loaders/globalloader.fsx"
// #endif

open Html


let menu (ctx : SiteContents) (page: string) =
  let shortcuts = ctx.GetValues<Pageloader.Shortcut> ()
  let all = ctx.GetValues<Apirefloader.AssemblyEntities>()

  let content = ctx.GetValues<Contentloader.Post> ()
  let siteInfo = ctx.TryGetValue<Globalloader.SiteInfo>().Value
  let rootUrl = siteInfo.root_url

  let group = content |> Seq.tryFind (fun n -> n.title = page) |> Option.map (fun n -> n.category)

  let explenations =
    content
    |> Seq.filter (fun n -> n.category = Contentloader.Explanation && not n.hide_menu )
    |> Seq.sortBy (fun n -> n.menu_order)

  let tutorials =
    content
    |> Seq.filter (fun n -> n.category = Contentloader.Tutorial && not n.hide_menu )
    |> Seq.sortBy (fun n -> n.menu_order)

  let howtos =
    content
    |> Seq.filter (fun n -> n.category = Contentloader.HowTo && not n.hide_menu )
    |> Seq.sortBy (fun n -> n.menu_order)

  let hasTutorials = not (Seq.isEmpty tutorials)
  let hasExplenations = not (Seq.isEmpty explenations)
  let hasHowTos = not (Seq.isEmpty howtos)

  let menuHeader =
    [
      if hasExplenations then
        li [Id "menu-explanations"; if group = Some Contentloader.Explanation then Class "dd-item menu-group-link menu-group-link-active" else  Class "dd-item menu-group-link"; ] [
          a [] [!! "Explanation"]
        ]
      if hasTutorials then
        li [Id "menu-tutorials"; if group = Some Contentloader.Tutorial then Class "dd-item menu-group-link menu-group-link-active" else Class "dd-item menu-group-link"; ] [
          a [] [!! "Tutorials"]
        ]
      if hasHowTos then
        li [Id "menu-howtos"; if group = Some Contentloader.HowTo then Class "dd-item menu-group-link menu-group-link-active" else Class "dd-item menu-group-link"; ] [
          a [] [!! "How-To Guides"]
        ]
      li [ Id "menu-refs"; if group = None then Class "dd-item menu-group-link menu-group-link-active" else Class "dd-item menu-group-link";] [
        a [] [!! "API References"]
      ]
    ]

  let renderExpls =
    ul [Id "submenu-explanations"; if group = Some Contentloader.Explanation then Class "submenu submenu-active" else Class "submenu"; ] [
      for r in explenations ->
        li [] [
          a [Href (rootUrl.subRoute r.link); if r.title = page then Class "active-link padding" else Class "padding"] [
            !! r.title
          ]
        ]
    ]

  let renderTuts =
    ul [Id "submenu-tutorials"; if group = Some Contentloader.Tutorial then Class "submenu submenu-active" else Class "submenu"; ] [
      for r in tutorials ->
        li [] [
          a [ Href (rootUrl.subRoute r.link); if r.title = page then Class "active-link padding" else Class "padding" ] [
            !! r.title
          ]
        ]
    ]

  let renderHowTos =
    ul [Id "submenu-howtos"; if group = Some Contentloader.HowTo then Class "submenu submenu-active" else Class "submenu"; ] [
      for r in howtos ->
        li [] [
          a [Href (rootUrl.subRoute "/" +  r.link); if r.title = page then Class "active-link padding" else Class "padding" ] [
            !! r.title
          ]
        ]
    ]

  let renderRefs =
    ul [Id "submenu-refs"; if group = None then Class "submenu submenu-active" else Class "submenu"; ] [
      for r in all ->
        li [] [
          a [Href (rootUrl.subRoute "/reference/" +  r.Label + "/index.html"); if r.Label = page then Class "active-link padding" else Class "padding" ] [
            !! r.Label
          ]
        ]
    ]

  let renderShortcuts =
    section [Id "shortcuts"] [
        h3 [] [!! "Shortcuts"]
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
      !! """<p>Built with <a href="https://github.com/ionide/Fornax">Fornax</a>"""
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
      script [Type "text/javascript"; Src "//cdnjs.cloudflare.com/ajax/libs/lunr.js/2.3.8/lunr.min.js"] []
      script [Type "text/javascript"; Src "//cdnjs.cloudflare.com/ajax/libs/JavaScript-autoComplete/1.0.4/auto-complete.min.js"] []
      script [Type "text/javascript";] [!! (sprintf "var baseurl ='%s'" (rootUrl.subRoute ""))]
      script [Type "text/javascript"; Src (rootUrl.subRoute "/static/js/search.js")] []
    ]
    div [Class "highlightable"] [
      ul [Class "topics"] menuHeader
      if hasExplenations then renderExpls
      if hasTutorials then renderTuts
      if hasHowTos then renderHowTos
      renderRefs
      renderShortcuts
      renderFooter
    ]
  ]

