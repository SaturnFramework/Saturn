#r "../_lib/Fornax.Core.dll"
#r "../../packages/docs/FSharp.Formatting/lib/netstandard2.0/FSharp.MetadataFormat.dll"

open System
open System.IO
open FSharp.MetadataFormat

type ApiPageInfo<'a> = {
    ParentName: string
    ParentUrlName: string
    NamespaceName: string
    NamespaceUrlName: string
    Info: 'a
}

let rec collectModules pn pu nn nu (m: Module) =
    [
        yield { ParentName = pn; ParentUrlName = pu; NamespaceName = nn; NamespaceUrlName = nu; Info =  m}
        yield! m.NestedModules |> List.collect (collectModules m.Name m.UrlName nn nu )
    ]


let loader (projectRoot: string) (siteContet: SiteContents) =
    let dll = Path.Combine(projectRoot, "..", "temp", "Saturn.dll")
    let libs =
      [
        Path.Combine (projectRoot, "..", "temp")
        Path.Combine (projectRoot, "..", "packages", "docsasp", "Microsoft.AspNetCore.app.ref", "ref", "netcoreapp3.1")
      ]
    let output = MetadataFormat.Generate(dll, markDownComments = true, publicOnly = true, libDirs = libs)

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


    allModules |> List.iter (siteContet.Add)
    allTypes |> List.iter (siteContet.Add)
    siteContet.Add(output)
    siteContet
