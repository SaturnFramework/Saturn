#r "../../_lib/Fornax.Core.dll"
#if !FORNAX
#load "../../loaders/contentloader.fsx"
#load "../../loaders/pageloader.fsx"
#load "../../loaders/globalloader.fsx"
#endif
#load "menu.fsx"
#load "header.fsx"
#load "footer.fsx"

open Html

let injectWebsocketCode (webpage:string) =
    let websocketScript =
        """
        <script type="text/javascript">
          var wsUri = "ws://localhost:8080/websocket";
      function init()
      {
        websocket = new WebSocket(wsUri);
        websocket.onclose = function(evt) { onClose(evt) };
      }
      function onClose(evt)
      {
        console.log('closing');
        websocket.close();
        document.location.reload();
      }
      window.addEventListener("load", init, false);
      </script>
        """
    let head = "<head>"
    let index = webpage.IndexOf head
    webpage.Insert ( (index + head.Length + 1),websocketScript)


let layout (ctx : SiteContents) bodyCnt (page: string) =

    html [Class "js csstransforms3d"] [
        Header.header ctx page
        body [] [
          Menu.menu ctx page
          section [Id "body"] [
            div [Id "overlay"] []
            div [ Class "padding highlightable"] [
              div [Id "body-inner"] [
                span [Id "sidebar-toggle-span"] [
                  a [Href "#"; Id "sidebar-toggle"; Custom("data-sidebar-toggle", "") ] [
                    i [Class "fas fa-bars"] []
                    !! " navigation"
                  ]
                ]
                yield! bodyCnt
              ]
            ]
          ]
          yield! Footer.footer ctx
        ]
    ]

let render (ctx : SiteContents) cnt  =
  let disableLiveRefresh = ctx.TryGetValue<Contentloader.PostConfig> () |> Option.map (fun n -> n.disableLiveRefresh) |> Option.defaultValue false
  cnt
  |> HtmlElement.ToString
  |> fun n -> if disableLiveRefresh then n else injectWebsocketCode n
