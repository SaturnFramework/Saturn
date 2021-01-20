#r "_lib/Fornax.Core.dll"

open Config

let customRename (page: string) =
    System.IO.Path.ChangeExtension(page.Replace ("content/", ""), ".html")

let isScriptToParse (ap, rp : string) =
    let folder = System.IO.Path.GetDirectoryName rp
    folder.Contains "content" && rp.EndsWith ".fsx"


let config = {
    Generators = [
        {Script = "page.fsx"; Trigger = OnFileExt ".md"; OutputFile = Custom customRename }
        {Script = "page.fsx"; Trigger = OnFilePredicate isScriptToParse; OutputFile = Custom customRename }
        {Script = "apiref.fsx"; Trigger = Once; OutputFile = MultipleFiles (sprintf "reference/%s.html") }

        {Script = "lunr.fsx"; Trigger = Once; OutputFile = NewFileName "index.json" }
    ]
}
