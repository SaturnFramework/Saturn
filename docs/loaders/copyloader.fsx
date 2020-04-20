#r "../_lib/Fornax.Core.dll"

open System.IO


let loader (projectRoot: string) (siteContet: SiteContents) =
    let intputPath = Path.Combine(projectRoot, "static")
    let outputPath = Path.Combine(projectRoot, "_public", "static")
    if Directory.Exists outputPath then Directory.Delete(outputPath, true)
    Directory.CreateDirectory outputPath |> ignore

    for dirPath in Directory.GetDirectories(intputPath, "*", SearchOption.AllDirectories) do
        Directory.CreateDirectory(dirPath.Replace(intputPath, outputPath)) |> ignore

    for filePath in Directory.GetFiles(intputPath, "*.*", SearchOption.AllDirectories) do
        File.Copy(filePath, filePath.Replace(intputPath, outputPath), true)

    let intputPath = Path.Combine(projectRoot, "assets")
    let outputPath = Path.Combine(projectRoot, "_public", "assets")
    if Directory.Exists outputPath then Directory.Delete(outputPath, true)
    Directory.CreateDirectory outputPath |> ignore

    for dirPath in Directory.GetDirectories(intputPath, "*", SearchOption.AllDirectories) do
        Directory.CreateDirectory(dirPath.Replace(intputPath, outputPath)) |> ignore

    for filePath in Directory.GetFiles(intputPath, "*.*", SearchOption.AllDirectories) do
        File.Copy(filePath, filePath.Replace(intputPath, outputPath), true)


    let intputPath = Path.Combine(projectRoot, "index.html")
    let outputPath = Path.Combine(projectRoot, "_public", "index.html")
    File.Copy(intputPath, outputPath, true)


    siteContet
