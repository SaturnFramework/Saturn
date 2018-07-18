// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------
#r "./packages/build/FAKE/tools/FakeLib.dll"
open Fake.Core
#load "paket-files/build/fsharp/FAKE/modules/Octokit/Octokit.fsx"




open Fake.Core
open Fake.DotNet
open Fake.Tools
open Fake.IO
open Fake.IO.Globbing.Operators
open System
open Octokit

// --------------------------------------------------------------------------------------
// Information about the project to be used at NuGet and in AssemblyInfo files
// --------------------------------------------------------------------------------------

let project = "Saturn"

let summary = "Opinionated, web development framework for F# which implements the server-side, functional MVC pattern"

let gitOwner = "Krzysztof-Cieslak"
let gitName = "Saturn"
let gitHome = "https://github.com/" + gitOwner


// --------------------------------------------------------------------------------------
// Build variables
// --------------------------------------------------------------------------------------

let buildDir  = "./build/"
let dotnetcliVersion = DotNet.getSDKVersionFromGlobalJson()

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let release = ReleaseNotes.parse (IO.File.ReadAllLines "RELEASE_NOTES.md")

// --------------------------------------------------------------------------------------
// Helpers
// --------------------------------------------------------------------------------------
let exec cmd args dir =
    if Process.execSimple( fun info ->

        { info with
            FileName = cmd
            WorkingDirectory =
                if String.IsNullOrWhiteSpace dir then info.WorkingDirectory
                else dir
            Arguments = args
            }
    ) System.TimeSpan.MaxValue <> 0 then
        failwithf "Error while running '%s' with args: %s" cmd args
let getBuildParam = Environment.environVar

let getUserInput =
// --------------------------------------------------------------------------------------
// Build Targets
// --------------------------------------------------------------------------------------

Target.create "Clean" (fun _ ->
    File.deleteAll [buildDir]
)

Target.create "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ AssemblyInfo.Title projectName
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, _, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        | Vbproj -> CreateVisualBasicAssemblyInfo ((folderName </> "My Project") </> "AssemblyInfo.vb") attributes
        | Shproj -> ()
        )
)

Target.create "InstallDotNetCLI" (fun _ ->
    let version = DotNet.CliVersion.Version dotnetcliVersion
    let options = DotNet.Options.Create()
    DotNet.install (fun opts -> { opts with Version = version }) options |> ignore
)

Target.create "Restore" (fun _ ->
    DotNet.restore id ""
)

Target.create "Build" (fun _ ->
    DotNet.build id ""
)

Target.create "Test" (fun _ ->
    exec "dotnet"  @"run --project .\tests\Saturn.UnitTests\Saturn.UnitTests.fsproj" "."
)

// --------------------------------------------------------------------------------------
// Release Targets
// --------------------------------------------------------------------------------------

Target.create "Pack" (fun _ ->
    Paket.pack (fun p ->
        { p with
            BuildConfig = "Release";
            OutputPath = buildDir;
            Version = release.NugetVersion
            ReleaseNotes = String.concat "\n" release.Notes
            MinimumFromLockFile = false
        }
    )
)

Target.create "ReleaseGitHub" (fun _ ->
    let remote =
        Git.CommandHelper.getGitResult "" "remote -v"
        |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
        |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
        |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

    Git.Staging.stageAll ""
    Git.Commit.exec "" (sprintf "Bump version to %s" release.NugetVersion)
    Git.Branches.pushBranch "" remote (Git.Information.getBranchName "")


    Git.Branches.tag "" release.NugetVersion
    Git.Branches.pushTag "" remote release.NugetVersion

    let client =
        let user =
            match getBuildParam "github-user" with
            | s when not (String.IsNullOrWhiteSpace s) -> s
            | _ -> getUserInput "Username: "
        let pw =
            match getBuildParam "github-pw" with
            | s when not (String.IsNullOrWhiteSpace s) -> s
            | _ -> getUserPassword "Password: "

        createClient user pw
    let file = !! (buildDir </> "*.nupkg") |> Seq.head

    // release on github
    client
    |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
    |> uploadFile file
    |> releaseDraft
    |> Async.RunSynchronously
)

Target "Push" (fun _ ->
    let key =
        match getBuildParam "nuget-key" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> getUserPassword "NuGet Key: "
    Paket.Push (fun p -> { p with WorkingDir = buildDir; ApiKey = key }))

// --------------------------------------------------------------------------------------
// Build order
// --------------------------------------------------------------------------------------
Target "Default" DoNothing
Target "Release" DoNothing

"Clean"
  ==> "InstallDotNetCLI"
  ==> "AssemblyInfo"
  ==> "Restore"
  ==> "Build"
  ==> "Test"
  ==> "Default"

"Default"
  ==> "Pack"
  ==> "ReleaseGitHub"
  ==> "Push"
  ==> "Release"

RunTargetOrDefault "Default"
