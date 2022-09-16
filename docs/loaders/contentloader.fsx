open System
#r "../_lib/Fornax.Core.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.CodeFormat.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.Markdown.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.Literate.dll"

// open FSharp.Literate
open FSharp.Formatting
// open FSharp.Formatting.Literate
// open FSharp.Formatting.Literate.Evaluation
open System.IO
// open FSharp.CodeFormat
open FSharp.Formatting.CodeFormat

type PostConfig = {
    disableLiveRefresh: bool
}

///This is following documentation structure described here https://documentation.divio.com/
type PostCategory =
  | Tutorial
  | Explanation
  | HowTo
  | TopLevel
  | ApiRef

with
  static member Parse x =
    match x with
    | "tutorial" -> Tutorial
    | "explanation" -> Explanation
    | "how-to" -> HowTo
    | "top-level" -> TopLevel
    | _ -> failwith "Unsupported category"

type Post = {
    file: string
    link : string
    title: string
    content: string
    text: string
    menu_order: int
    hide_menu: bool
    category: PostCategory
}

let tokenToCss (x: TokenKind) =
    match x with
    | TokenKind.Keyword -> "hljs-keyword"
    | TokenKind.String -> "hljs-string"
    | TokenKind.Comment -> "hljs-comment"
    | TokenKind.Identifier -> "hljs-function"
    | TokenKind.Inactive -> ""
    | TokenKind.Number -> "hljs-number"
    | TokenKind.Operator -> "hljs-keyword"
    | TokenKind.Punctuation -> "hljs-keyword"
    | TokenKind.Preprocessor -> "hljs-comment"
    | TokenKind.Module -> "hljs-type"
    | TokenKind.ReferenceType -> "hljs-type"
    | TokenKind.ValueType -> "hljs-type"
    | TokenKind.Interface -> "hljs-type"
    | TokenKind.TypeArgument -> "hljs-type"
    | TokenKind.Property -> "hljs-function"
    | TokenKind.Enumeration -> "hljs-type"
    | TokenKind.UnionCase -> "hljs-type"
    | TokenKind.Function -> "hljs-function"
    | TokenKind.Pattern -> "hljs-function"
    | TokenKind.MutableVar -> "hljs-symbol"
    | TokenKind.Disposable -> "hljs-symbol"
    | TokenKind.Printf -> "hljs-regexp"
    | TokenKind.Escaped -> "hljs-regexp"
    | TokenKind.Default -> ""




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
let getContent (fileContent : string) (fn: string) =
    let fileContent = fileContent.Split '\n'
    let fileContent = fileContent |> Array.skip 1 //First line must be ---
    let indexOfSeperator = fileContent |> Array.findIndex isSeparator
    let _, content = fileContent |> Array.splitAt indexOfSeperator

    let content = content |> Array.skip 1 |> String.concat "\n"
    let doc = Literate.ParseMarkdownFile fn
    let ps =
         doc.Paragraphs
        |> List.skip 3 //Skip opening ---, config content, and closing ---
    let doc = doc.With(paragraphs = ps)
    let html = Literate.WriteHtml(doc, lineNumbers = false, tokenKindToCss = tokenToCss)
                       .Replace("lang=\"fsharp", "class=\"language-fsharp")
    content, html

let trimString (str : string) =
    str.Trim().TrimEnd('"').TrimStart('"')

let relative toPath fromPath =
    let toUri = Uri(toPath)
    let fromUri = Uri(fromPath)
    toUri.MakeRelativeUri(fromUri).OriginalString

let loadFile projectRoot n =
    let text = System.IO.File.ReadAllText n

    let config = (getConfig text).Split( '\n') |> List.ofArray

    let (text, content) = getContent text n

    let file = relative (Path.Combine(projectRoot, "content") + string Path.DirectorySeparatorChar) n
    let link = Path.ChangeExtension(file, ".html")

    let title = config |> List.find (fun n -> n.ToLower().StartsWith "title" ) |> fun n -> n.Split(':').[1] |> trimString

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

    let category =
        let n = config |> List.find (fun n -> n.ToLower().StartsWith "category" )
        n.Split(':').[1] |> trimString |> PostCategory.Parse


    { file = file
      link = link
      title = title
      content = content
      menu_order = menu_order
      hide_menu = hide
      text = text
      category = category }

let loader (projectRoot: string) (siteContent: SiteContents) =
    try
        let postsPath = System.IO.Path.Combine(projectRoot, "content")
        let posts =
            Directory.GetFiles(postsPath, "*", SearchOption.AllDirectories )
            |> Array.filter (fun n -> n.EndsWith ".md")
            |> Array.map (loadFile projectRoot)

        posts
        |> Array.iter siteContent.Add

        siteContent.Add({disableLiveRefresh = true})
    with
    | ex -> printfn "EX: %A" ex

    siteContent

