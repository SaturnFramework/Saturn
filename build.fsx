// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------
#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Api

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
// Helpers
// --------------------------------------------------------------------------------------
let isNullOrWhiteSpace = System.String.IsNullOrWhiteSpace
let exec cmd args dir =
    if Process.execSimple( fun info ->

        { info with
            FileName = cmd
            WorkingDirectory =
                if (isNullOrWhiteSpace dir) then info.WorkingDirectory
                else dir
            Arguments = args
            }
    ) System.TimeSpan.MaxValue <> 0 then
        failwithf "Error while running '%s' with args: %s" cmd args
let getBuildParam = Environment.environVar

let DoNothing = ignore
// --------------------------------------------------------------------------------------
// Build Targets
// --------------------------------------------------------------------------------------

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [buildDir; tempDir]
)


Target.create "Restore" (fun _ ->
    DotNet.restore id ""
)

Target.create "Build" (fun _ ->
    DotNet.build id ""
)

Target.create "Publish" (fun _ ->
    !! "src/**/*.??proj"
    |> Seq.iter (fun n ->
        DotNet.publish (fun c ->
            {c with
                OutputPath = Some tempDir
                Configuration = DotNet.BuildConfiguration.Release
                Framework = Some "net5.0"
            }
        ) n
    )
)

Target.create "Docs" (fun _ ->
  Shell.cleanDirs ["docs\\_public";]
  exec "dotnet"  @"fornax build" "docs"
)

Target.create "Test" (fun _ ->
    exec "dotnet"  @"run --project .\tests\Saturn.UnitTests\Saturn.UnitTests.fsproj -c Release -- --summary" "."
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

Target.create "Push" (fun _ ->
    let key =
        match getBuildParam "nuget-key" with
        | s when not (isNullOrWhiteSpace s) -> s
        | _ -> UserInput.getUserPassword "NuGet Key: "
    Paket.push (fun p -> { p with WorkingDir = buildDir; ApiKey = key; ToolType = ToolType.CreateLocalTool() }))

// --------------------------------------------------------------------------------------
// Build order
// --------------------------------------------------------------------------------------
Target.create "Default" DoNothing
Target.create "Release" DoNothing

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

Target.runOrDefault "Build"
