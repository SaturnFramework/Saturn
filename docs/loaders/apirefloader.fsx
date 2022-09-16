#r "../_lib/Fornax.Core.dll"
#r "../../packages/docs/Newtonsoft.Json/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.ApiDocs.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.CodeFormat.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.Common.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.Literate.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.1/FSharp.Formatting.Markdown.dll"

open System
open System.IO
open FSharp.Formatting.ApiDocs

type ApiPageInfo<'a> = {
    ParentName: string
    ParentUrlName: string
    NamespaceName: string
    NamespaceUrlName: string
    Info: 'a
}

type AssemblyEntities = {
  Label: string
  Modules: ApiPageInfo<ApiDocEntity> list
  Types: ApiPageInfo<ApiDocEntity> list
  GeneratorOutput: GenerationPhase
}

let rec collectModules pn pu nn nu (m: ApiDocEntity) =
    [
        yield { ParentName = pn; ParentUrlName = pu; NamespaceName = nn; NamespaceUrlName = nu; Info =  m}
        yield! m.NestedModules |> List.collect (collectModules m.Name m.UrlName nn nu )
    ]


let loader (projectRoot: string) (siteContet: SiteContents) =
    try
      let dlls =
        [
          "Saturn", Path.Combine(projectRoot, "..", "temp", "Saturn.dll")
          "Saturn.AzureFunctions", Path.Combine(projectRoot, "..", "temp", "Saturn.AzureFunctions.dll")
          "Saturn.Extensions.Authorization", Path.Combine(projectRoot, "..", "temp", "Saturn.Extensions.Authorization.dll")
          "Saturn.Extensions.gRPC", Path.Combine(projectRoot, "..", "temp", "Saturn.Extensions.gRPC.dll")
          "Saturn.Extensions.HttpSys", Path.Combine(projectRoot, "..", "temp", "Saturn.Extensions.HttpSys.dll")
          "Saturn.Extensions.Turbolinks", Path.Combine(projectRoot, "..", "temp", "Saturn.Extensions.Turbolinks.dll")
        ]
      let libs =
        [
          Path.Combine (projectRoot, "..", "temp")
          Path.Combine (projectRoot, "..", "packages", "docsasp", "Microsoft.AspNetCore.app.ref", "ref", "net6.0")
        ]
      let sourceFolder = Path.Combine(projectRoot, "..")
      for (label, dll) in dlls do
        let output = MetadataFormat.Generate(dll, markDownComments = true, publicOnly = true, libDirs = libs, sourceRepo = "https://github.com/SaturnFramework/Saturn/tree/master", sourceFolder = sourceFolder)

        let allModules =
            output.AssemblyGroup.Namespaces
            |> List.collect (fun n ->
                List.collect (collectModules n.Name n.Name n.Name n.Name) n.Modules
            )

        let allTypes =
            [
                yield!
                    output.AssemblyGroup.Namespaces
                    |> List.collect (fun n ->
                        n.Types |> List.map (fun t -> {ParentName = n.Name; ParentUrlName = n.Name; NamespaceName = n.Name; NamespaceUrlName = n.Name; Info = t} )
                    )
                yield!
                    allModules
                    |> List.collect (fun n ->
                        n.Info.NestedTypes |> List.map (fun t -> {ParentName = n.Info.Name; ParentUrlName = n.Info.UrlName; NamespaceName = n.NamespaceName; NamespaceUrlName = n.NamespaceUrlName; Info = t}) )
            ]
        let entities = {
          Label = label
          Modules = allModules
          Types = allTypes
          GeneratorOutput = output
        }
        siteContet.Add entities
    with
    | ex ->
      printfn "%A" ex

    siteContet
