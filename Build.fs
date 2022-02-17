module Build

open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Api
open System
open System.IO
open Fake.Core
open Fake.DotNet
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools
open Helpers

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let project = "Saturn"

let summary = "Opinionated, web development framework for F# which implements the server-side, functional MVC pattern"

let gitOwner = "SaturnFramework"
let gitName = "Saturn"
let gitHome = "https://github.com/" + gitOwner
let gitUrl = gitHome + "/" + gitName

// --------------------------------------------------------------------------------------
// Build variables
// --------------------------------------------------------------------------------------

let buildDir  = "./build/"
let tempDir  = "./temp/"

System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let changelogFilename = "CHANGELOG.md"
let changelog = Changelog.load changelogFilename
let latestEntry = changelog.LatestEntry

// --------------------------------------------------------------------------------------
// Standard DotNet Build Steps
// --------------------------------------------------------------------------------------
let install = lazy DotNet.install DotNet.Versions.FromGlobalJson
let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

// --------------------------------------------------------------------------------------
// Build Targets
// --------------------------------------------------------------------------------------

let init args =
    initializeContext args
    Target.create "Clean" (fun _ ->
        Shell.cleanDirs [buildDir; tempDir]
    )


    Target.create "Restore" (fun _ ->
        DotNet.restore id ""
    )

    Target.create "Build" (fun _ ->
        !! "src/**/*.fsproj"
        |> Seq.filter (fun s ->
            let name = Path.GetDirectoryName s
            not (name.Contains "docs"))
        |> Seq.iter (fun s ->
            let dir = Path.GetDirectoryName s
            DotNet.build id dir)
    )

    Target.create "Publish" (fun _ ->
        !! "src/**/*.??proj"
        |> Seq.iter (fun n ->
            DotNet.publish (fun c ->
                {c with
                    OutputPath = Some tempDir
                    Configuration = DotNet.BuildConfiguration.Release
                    Framework = Some "net6.0"
                }
            ) n
        )
    )

    Target.create "Docs" (fun _ ->
    Shell.cleanDirs ["docs\\_public";]
    runDotNet @"fornax build" "docs"
    )

    Target.create "Test" (fun _ ->
        runDotNet @"run --project .\tests\Saturn.UnitTests\Saturn.UnitTests.fsproj -c Release -- --summary" "."
    )

    // --------------------------------------------------------------------------------------
    // Release Targets
    // --------------------------------------------------------------------------------------

    Target.create "Pack" (fun _ ->
        let releaseNotes = sprintf "%s/blob/v%s/CHANGELOG.md" gitUrl latestEntry.NuGetVersion
        DotNet.pack (fun p ->
            { p with
                Configuration = DotNet.BuildConfiguration.Release
                OutputPath = Some buildDir
                MSBuildParams = { p.MSBuildParams with Properties = [("Version", latestEntry.NuGetVersion); ("PackageReleaseNotes", releaseNotes)]}
            }
        ) "Saturn.sln"
    )

    Target.create "ReleaseGitHub" (fun _ ->
        let remote =
            Git.CommandHelper.getGitResult "" "remote -v"
            |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
            |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
            |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

        Git.Staging.stageAll ""
        Git.Commit.exec "" (sprintf "Bump version to %s" latestEntry.NuGetVersion)
        Git.Branches.pushBranch "" remote (Git.Information.getBranchName "")


        Git.Branches.tag "" (sprintf "v%s" latestEntry.NuGetVersion)
        Git.Branches.pushTag "" remote (sprintf "v%s" latestEntry.NuGetVersion)
    )

    let getBuildParam = Environment.environVar
    let isNullOrWhiteSpace = String.IsNullOrWhiteSpace

    // Workaround for https://github.com/fsharp/FAKE/issues/2242
    let pushPackage _ =
        let nugetCmd fileName key = sprintf "nuget push %s -k %s -s nuget.org" fileName key
        let key =
            //Environment.environVarOrFail "nugetKey"
            match getBuildParam "nugetkey" with
            | s when not (isNullOrWhiteSpace s) -> s
            | _ -> UserInput.getUserPassword "NuGet Key: "
        IO.Directory.GetFiles(buildDir, "*.nupkg", SearchOption.TopDirectoryOnly)
        |> Seq.map Path.GetFileName
        |> Seq.iter (fun fileName ->
            Trace.tracef "fileName %s" fileName
            let cmd = nugetCmd fileName key
            runDotNet cmd buildDir)
    Target.create "Push" (fun _ -> pushPackage [] )


    let DoNothing = ignore

    // --------------------------------------------------------------------------------------
    // Build order
    // --------------------------------------------------------------------------------------
    Target.create "Default" DoNothing
    Target.create "Release" DoNothing

    let dependencies =[
        "Clean"
            ==> "Build"
            ==> "Test"
            ==> "Default"

        "Clean"
            ==> "Publish"
            ==> "Docs"

        "Default"
            ==> "Pack"
            ==> "ReleaseGitHub"
            ==> "Release"
        "Pack"
            ==> "Push"

        "Build"
    ]
    ()

[<EntryPoint>]
let main args =
    init((args |> List.ofArray))
    try
        Target.runOrDefaultWithArguments "Test"
        0
    with e ->
        printfn "%A" e
        1
