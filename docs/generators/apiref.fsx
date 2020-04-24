#r "../_lib/Fornax.Core.dll"
#r "../../packages/docs/Markdig/lib/netstandard2.0/Markdig.dll"
#r "../../packages/docs/Newtonsoft.Json/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.0/FSharp.MetadataFormat.dll"

#if !FORNAX
#load "../loaders/apirefloader.fsx"
#endif

#load "partials/layout.fsx"

open System
open FSharp.MetadataFormat
open Html
open Apirefloader
open Markdig

let markdownPipeline =
    MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseGridTables()
        .Build()

let getComment (c: Comment) =
  let t =
    c.RawData
    |> List.map (fun n -> n.Value)
    |> String.concat "\n\n"
  Markdown.ToHtml(t, markdownPipeline)


let formatMember (m: Member) =
    let attributes =
      m.Attributes
      |> List.filter (fun a -> a.FullName <> "Microsoft.FSharp.Core.CustomOperationAttribute")

    let hasCustomOp =
      m.Attributes
      |> List.exists (fun a -> a.FullName = "Microsoft.FSharp.Core.CustomOperationAttribute")

    let customOp =
      if hasCustomOp then
        m.Attributes
        |> List.tryFind (fun a -> a.FullName = "Microsoft.FSharp.Core.CustomOperationAttribute")
        |> Option.bind (fun a ->
          a.ConstructorArguments
          |> Seq.tryFind (fun x -> x :? string)
          |> Option.map (fun x -> x.ToString())
        )
        |> Option.defaultValue ""
      else
        ""

    tr [] [
        td [] [
            code [] [!! m.Name]
            br []

            if hasCustomOp then
              b [] [!! "CE Custom Operation: "]
              code [] [!!customOp]
              br []
            br []
            b [] [!! "Signature: "]
            !!m.Details.Signature
            br []
            if not (attributes.IsEmpty) then
                b [] [!! "Attributes:"]
                for a in attributes do
                    code [] [!! (a.Name)]
        ]
        td [] [!! (getComment m.Comment)]
    ]

let generateType ctx (page: ApiPageInfo<Type>) =
    let t = page.Info
    let body =
        div [Class "api-page"] [
            h2 [] [!! t.Name]
            b [] [!! "Namespace: "]
            a [Href (sprintf "%s.html" page.NamespaceUrlName) ] [!! page.NamespaceName]
            br []
            b [] [!! "Parent: "]
            a [Href (sprintf "%s.html" page.ParentUrlName)] [!! page.ParentName]
            span [] [!! (getComment t.Comment)]
            br []
            if not (String.IsNullOrWhiteSpace t.Category) then
                b [] [!! "Category:"]
                !!t.Category
                br []
            if not (t.Attributes.IsEmpty) then
                b [] [!! "Attributes:"]
                for a in t.Attributes do
                    br []
                    code [] [!! (a.Name)]
                br []

            table [] [
                tr [] [
                    th [ Width "35%" ] [!!"Name"]
                    th [ Width "65%"] [!!"Description"]
                ]
                if not t.Constructors.IsEmpty then tr [] [ td [ColSpan 3. ] [ b [] [!! "Constructors"]]]
                yield! t.Constructors |> List.map formatMember

                if not t.InstanceMembers.IsEmpty then tr [] [ td [ColSpan 3. ] [ b [] [!! "Instance Members"]]]
                yield! t.InstanceMembers |> List.map formatMember

                if not t.RecordFields.IsEmpty then tr [] [ td [ColSpan 3. ] [ b [] [!! "Record Fields"]]]
                yield! t.RecordFields |> List.map formatMember

                if not t.StaticMembers.IsEmpty then tr [] [ td [ColSpan 3. ] [ b [] [!! "Static Members"]]]
                yield! t.StaticMembers |> List.map formatMember

                if not t.StaticParameters.IsEmpty then tr [] [ td [ColSpan 3. ] [ b [] [!! "Static Parameters"]]]
                yield! t.StaticParameters |> List.map formatMember

                if not t.UnionCases.IsEmpty then tr [] [ td [ColSpan 3. ] [ b [] [!! "Union Cases"]]]
                yield! t.UnionCases |> List.map formatMember
            ]
        ]
    t.UrlName, Layout.layout ctx [body] t.Name

let generateModule ctx (page: ApiPageInfo<Module>) =
    let m = page.Info
    let body =
        div [Class "api-page"] [
            h2 [] [!!m.Name]
            b [] [!! "Namespace: "]
            a [Href (sprintf "%s.html" page.NamespaceUrlName) ] [!! page.NamespaceName]
            br []
            b [] [!! "Parent: "]
            a [Href (sprintf "%s.html" page.ParentUrlName)] [!! page.ParentName]
            span [] [!! (getComment m.Comment)]
            br []
            if not (String.IsNullOrWhiteSpace m.Category) then
                b [] [!! "Category:"]
                !!m.Category
                br []

            if not m.NestedTypes.IsEmpty then
                b [] [!! "Declared Types"]
                table [] [
                    tr [] [
                        th [ Width "35%" ] [!!"Type"]
                        th [ Width "65%"] [!!"Description"]
                    ]
                    for t in m.NestedTypes do
                        tr [] [
                            td [] [a [Href (sprintf "%s.html" t.UrlName )] [!! t.Name ]]
                            td [] [!! (getComment t.Comment)]
                        ]
                ]
                br []

            if not m.NestedModules.IsEmpty then
                b [] [!! "Declared Modules"]
                table [] [
                    tr [] [
                        th [ Width "35%" ] [!!"Module"]
                        th [ Width "65%"] [!!"Description"]
                    ]
                    for t in m.NestedModules do
                        tr [] [
                            td [] [a [Href (sprintf "%s.html" t.UrlName )] [!! t.Name ]]
                            td [] [!! (getComment t.Comment)]
                        ]
                ]
                br []

            if not m.ValuesAndFuncs.IsEmpty then
                b [] [!! "Values and Functions"]
                table [] [
                    tr [] [
                        th [ Width "35%" ] [!!"Name"]
                        th [ Width "65%"] [!!"Description"]
                    ]
                    yield! m.ValuesAndFuncs |> List.map formatMember
                ]
                br []

            if not m.TypeExtensions.IsEmpty then
                b [] [!! "Type Extensions"]
                table [] [
                    tr [] [
                        th [ Width "35%" ] [!!"Name"]
                        th [ Width "65%"] [!!"Description"]
                    ]
                    yield! m.TypeExtensions |> List.map formatMember
                ]
        ]
    m.UrlName, Layout.layout ctx [body] m.Name

let generateNamespace ctx (n: Namespace)  =
    let body =
        div [Class "api-page"] [
            h2 [] [!!n.Name]

            if not n.Types.IsEmpty then

                b [] [!! "Declared Types"]
                table [] [
                    tr [] [
                        th [ Width "35%" ] [!!"Type"]
                        th [ Width "65%"] [!!"Description"]
                    ]
                    for t in n.Types do
                        tr [] [
                            td [] [a [Href (sprintf "%s.html" t.UrlName )] [!! t.Name ]]
                            td [] [!!(getComment t.Comment)]
                        ]
                ]
                br []

            if not n.Modules.IsEmpty then

                b [] [!! "Declared Modules"]
                table [] [
                    tr [] [
                        th [ Width "35%" ] [!!"Module"]
                        th [ Width "65%"] [!!"Description"]
                    ]
                    for t in n.Modules do
                        tr [] [
                            td [] [a [Href (sprintf "%s.html" t.UrlName )] [!! t.Name ]]
                            td [] [!! (getComment t.Comment)]
                        ]
                ]
        ]
    n.Name, Layout.layout ctx [body] (n.Name)


let generate' (ctx : SiteContents)  =
    let all = ctx.TryGetValues<AssemblyEntities>()
    match all with
    | None -> []
    | Some all ->
      all
      |> Seq.toList
      |> List.collect (fun n ->
        let name = n.GeneratorOutput.AssemblyGroup.Name
        let namespaces =
          n.GeneratorOutput.AssemblyGroup.Namespaces
          |> List.map (generateNamespace ctx)

        let modules =
          n.Modules
          |> Seq.map (generateModule ctx)

        let types =
          n.Types
          |> Seq.map (generateType ctx)

        let ref =
          Layout.layout ctx [
            h1 [] [!! name ]
            b [] [!! "Declared namespaces"]
            br []
            for (n, _) in namespaces do
                a [Href (sprintf "%s.html"  n)] [!!n]
                br []
          ] n.Label

        [("index" , ref); yield! namespaces; yield! modules; yield! types]
        |> List.map (fun (x, y) -> (sprintf "%s/%s" n.Label x), y)
      )


let generate (ctx : SiteContents) (projectRoot: string) (page: string) =
    try
        generate' ctx
        |> List.map (fun (n,b) -> n, (Layout.render ctx b))
    with
    | ex ->
        printfn "ERROR IN API REF GENERATION:\n%A" ex
        []
