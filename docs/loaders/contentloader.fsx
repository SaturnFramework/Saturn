open System
#r "../_lib/Fornax.Core.dll"
#r "../../packages/docs/Markdig/lib/netstandard2.0/Markdig.dll"

open Markdig
open System.IO

type PostConfig = {
    disableLiveRefresh: bool
}
type Post = {
    file: string
    link : string
    title: string
    author: string option
    published: System.DateTime option
    tags: string list
    content: string
    text: string
    menu_order: int
    hide_menu: bool
}


let markdownPipeline =
    MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseGridTables()
        .Build()

let isSeparator (input : string) =
    input.StartsWith "---"

///`fileContent` - content of page to parse. Usually whole content of `.md` file
///returns content of config that should be used for the page
let getConfig (fileContent : string) =
    let fileContent = fileContent.Split '\n'
    let fileContent = fileContent |> Array.skip 1 //First line must be ---
    let indexOfSeperator = fileContent |> Array.findIndex isSeparator
    fileContent
    |> Array.splitAt indexOfSeperator
    |> fst
    |> String.concat "\n"

///`fileContent` - content of page to parse. Usually whole content of `.md` file
///returns HTML version of content of the page
let getContent (fileContent : string) =
    let fileContent = fileContent.Split '\n'
    let fileContent = fileContent |> Array.skip 1 //First line must be ---
    let indexOfSeperator = fileContent |> Array.findIndex isSeparator
    let _, content = fileContent |> Array.splitAt indexOfSeperator

    let content = content |> Array.skip 1 |> String.concat "\n"
    content, Markdown.ToHtml(content, markdownPipeline)

let trimString (str : string) =
    str.Trim().TrimEnd('"').TrimStart('"')

let relative toPath fromPath =
    let toUri = Uri(toPath)
    let fromUri = Uri(fromPath)
    toUri.MakeRelativeUri(fromUri).OriginalString

let loadFile projectRoot n =
    let text = System.IO.File.ReadAllText n

    let config = (getConfig text).Split( '\n') |> List.ofArray

    let (text, content) = getContent text

    let file = relative projectRoot n
        // let relativePath
        // System.IO.Path.Combine("content", (n |> System.IO.Path.GetFileNameWithoutExtension) + ".md").Replace("\\", "/")
    let link = Path.ChangeExtension(file, ".html")

    let title = config |> List.find (fun n -> n.ToLower().StartsWith "title" ) |> fun n -> n.Split(':').[1] |> trimString

    let author =
        try
            config |> List.tryFind (fun n -> n.ToLower().StartsWith "author" ) |> Option.map (fun n -> n.Split(':').[1] |> trimString)
        with
        | _ -> None

    let published =
        try
            config |> List.tryFind (fun n -> n.ToLower().StartsWith "published" ) |> Option.map (fun n -> n.Split(':').[1] |> trimString |> System.DateTime.Parse)
        with
        | _ -> None

    let menu_order =
        try
            let n = config |> List.find (fun n -> n.ToLower().StartsWith "menu_order" )
            n.Split(':').[1] |> trimString |> System.Int32.Parse
        with
        | _ -> 10

    let hide =
        try
            let n = config |> List.find (fun n -> n.ToLower().StartsWith "hide_menu" )
            n.Split(':').[1] |> trimString |> System.Boolean.Parse
        with
        | _ -> false


    let tags =
        try
            let x =
                config
                |> List.tryFind (fun n -> n.ToLower().StartsWith "tags" )
                |> Option.map (fun n -> n.Split(':').[1] |> trimString |> fun n -> n.Split ',' |> Array.toList )
            defaultArg x []
        with
        | _ -> []

    { file = file
      link = link
      title = title
      author = author
      published = published
      tags = tags
      content = content
      menu_order = menu_order
      hide_menu = hide
      text = text }

type FsTree = Node of (string * FsTree) list

let rec addPath (p : string list)   (Node ns) =
    match p with
    | []       -> Node                    ns
    | hp :: tp -> Node (addHeadPath hp tp ns)

and addHeadPath hp tp ns =
    match ns with
    | []                          -> [hp, addPath tp (Node[]) ]
    | (nn, st) :: tn when nn = hp -> (nn, addPath tp st       ) ::                   tn
    | hn       :: tn              -> hn                         :: addHeadPath hp tp tn


let loader (projectRoot: string) (siteContet: SiteContents) =
    try
        let postsPath = System.IO.Path.Combine(projectRoot, "content")
        let posts =
            Directory.GetFiles(postsPath, "*", SearchOption.AllDirectories )
            |> Array.filter (fun n -> n.EndsWith ".md")
            |> Array.map (loadFile projectRoot)

        posts
        |> Array.iter (fun p -> siteContet.Add p)

        let tree =
            (Node [], posts)
            ||> Seq.fold (fun acc e ->
                let path = e.link.Replace("content/", "").Split('/') |> List.ofArray
                addPath path acc)

        siteContet.Add(tree)
        siteContet.Add({disableLiveRefresh = true})
    with
    | ex -> printfn "EX: %A" ex

    siteContet