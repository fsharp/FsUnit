#r @"paket:
source https://nuget.org/api/v2
framework netstandard2.0
nuget Fantomas prerelease
nuget Fake.Core.Target
nuget Fake.Core.Trace
nuget Fake.Core.ReleaseNotes
nuget Fake.IO.FileSystem
nuget Fake.DotNet.Cli
nuget Fake.DotNet.MSBuild
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Paket
nuget Fake.DotNet.Fsi
nuget Fake.Tools.Git
nuget Fake.Api.GitHub //"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif

open Fake
open Fake.Core.TargetOperators
open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.Tools.Git
open System.IO
open Fantomas.FakeHelpers

Target.initEnvironment()

// --------------------------------------------------------------------------------------
// Project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - by the generated NuGet package
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project
// (used by attributes in AssemblyInfo, name of a NuGet package and directory in 'src')
let project = "FsUnit"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "FsUnit is a set of libraries that makes unit-testing with F# more enjoyable."

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "fsprojects"
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "FsUnit"

// The url for the raw files hosted
//let gitRaw = environVarOrDefault "gitRaw" "https://raw.github.com/fsprojects"
let gitRaw = "https://raw.github.com/fsprojects"
let cloneUrl = "git@github.com:fsprojects/FsUnit.git"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

// Read additional information from the release notes document
let release = ReleaseNotes.load "RELEASE_NOTES.md"

// Helper active pattern for project types
let (|Fsproj|Csproj|Vbproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

// Generate assembly info files with the right version & up-to-date information
Target.create "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ AssemblyInfo.Title (projectName)
          AssemblyInfo.Product project
          AssemblyInfo.Description summary
          AssemblyInfo.Version release.AssemblyVersion
          AssemblyInfo.FileVersion release.AssemblyVersion ]

    let getProjectDetails (projectPath:string) =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.filter (fun x -> not <| x.Contains(".netstandard"))
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, _, folderName, attributes) ->
        match projFileName with
        | Fsproj -> AssemblyInfoFile.createFSharp (folderName @@ "AssemblyInfo.fs") attributes
        | Csproj -> AssemblyInfoFile.createCSharp ((folderName @@ "Properties") @@ "AssemblyInfo.cs") attributes
        | Vbproj -> AssemblyInfoFile.createVisualBasic ((folderName @@ "My Project") @@ "AssemblyInfo.vb") attributes
        )
)

// Copies binaries from default VS location to expected bin folder
// But keeps a subdirectory structure for each project in the
// src folder to support multiple project outputs
Target.create "CopyBinaries" (fun _ ->
    !! "src/**/*.??proj"
    |>  Seq.map (fun f -> ((Path.GetDirectoryName f) @@ "bin/Release", "bin" @@ (Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> Shell.copyDir toDir fromDir (fun _ -> true))
)

// --------------------------------------------------------------------------------------
// Clean build results

Target.create "Clean" (fun _ ->
    Shell.cleanDirs
        [
        "bin"; "temp";
        "src/FsUnit.NUnit/bin/";
        "src/FsUnit.NUnit/obj/";
        "src/FsUnit.Xunit/bin/";
        "src/FsUnit.Xunit/obj/";
        "src/FsUnit.MsTestUnit/bin/"
        "src/FsUnit.MsTestUnit/obj/"
        ]
)

Target.create "CleanDocs" (fun _ ->
    Shell.cleanDirs ["docs/output"]
)

// --------------------------------------------------------------------------------------
// Check code format & format code using Fantomas

Target.create "Format" (fun _ ->
    !! "src/**/*.fs"
      ++ "tests/**/*.fs" 
      -- "./**/*AssemblyInfo.fs"
    |> formatCode
    |> Async.RunSynchronously
    |> printfn "Formatted files: %A"
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target.create "Build" (fun _ ->
    let result = DotNet.exec id "build" "FsUnit.sln -c Release"
    if not result.OK
    then failwithf "Build failed: %A" result.Errors
)

// --------------------------------------------------------------------------------------
// Run the unit tests using test runner

Target.create "NUnit" (fun _ ->
    DotNet.test id "tests/FsUnit.NUnit.Test/"
)

Target.create "xUnit" (fun _ ->
    DotNet.test id "tests/FsUnit.Xunit.Test/"
)

Target.create "MsTest" (fun _ ->
    DotNet.test id "tests/FsUnit.MsTest.Test/"
)

Target.create "RunTests" ignore

// --------------------------------------------------------------------------------------
// Build a NuGet package

Target.create "NuGet" (fun _ ->
    Paket.pack(fun p ->
        { p with
            ToolType = ToolType.CreateLocalTool()
            OutputPath = "bin"
            Version = release.NugetVersion
            ReleaseNotes = String.toLines release.Notes})
)

Target.create "PublishNuget" (fun _ ->
    Paket.push(fun p ->
        { p with
            ToolType = ToolType.CreateLocalTool()
            WorkingDir = "bin" })
)


// --------------------------------------------------------------------------------------
// Generate the documentation

let preGenerateDocs ()  =
    Shell.rm "docs/content/release-notes.md"
    Shell.copyFile "docs/content/" "RELEASE_NOTES.md"
    Shell.rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"

    Shell.rm "docs/content/license.md"
    Shell.copyFile "docs/content/" "LICENSE.txt"
    Shell.rename "docs/content/license.md" "docs/content/LICENSE.txt"

let generateDocs () =
    let result =
        DotNet.exec (fun p -> { p with WorkingDirectory = "." })
            "fsi" (__SOURCE_DIRECTORY__ + "/docs/generate.fsx")
    if result.OK then
        Trace.traceImportant "Help generated"
    else
        String.concat "\n" result.Errors
        |> failwithf "generating help documentation failed:\n%s"

Target.create "GenerateDocs" (fun _ ->
    preGenerateDocs()
    System.Environment.SetEnvironmentVariable("CHANGED_FILE", null)
    generateDocs()
)

Target.create "KeepRunning" (fun _ ->
    let watcherEventHandler (e:FileSystemEventArgs) =
        Trace.traceImportant <| sprintf "File %s is %A" e.FullPath e.ChangeType
        if not <| e.FullPath.Contains("output") then
            let value = if e.FullPath.EndsWith("generate.fsx") then null else e.FullPath
            System.Environment.SetEnvironmentVariable("CHANGED_FILE", value)
            generateDocs()

    use watcher = new FileSystemWatcher(DirectoryInfo("docs/").FullName,"*.*")
    watcher.Changed.Add(watcherEventHandler)
    watcher.Created.Add(watcherEventHandler)
    watcher.Renamed.Add(watcherEventHandler)
    watcher.Deleted.Add(watcherEventHandler)
    watcher.IncludeSubdirectories <- true
    watcher.EnableRaisingEvents <- true

    CreateProcess.fromRawCommandLine "dotnet" "serve -o -d ./docs/output"
    |> (Proc.run >> ignore)
    //Trace.traceImportant "Waiting for docs edits. Press any key to stop."
    //System.Console.ReadKey() |> ignore
    //watcher.EnableRaisingEvents <- false
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target.create "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    Shell.cleanDir tempDocsDir
    Repository.cloneSingleBranch "" cloneUrl "gh-pages" tempDocsDir

    Repository.fullclean tempDocsDir
    Shell.copyRecursive "docs/output" tempDocsDir true |> Trace.tracefn "%A"
    Staging.stageAll tempDocsDir
    Commit.exec tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target.create "All" ignore
Target.create "Release" ignore

"Clean"
  ==> "AssemblyInfo"
  ==> "Format"
  ==> "Build"
  ==> "CopyBinaries"
  ==> "RunTests"
  ==> "All"
  =?> ("ReleaseDocs", BuildServer.isLocalBuild)
  ==> "Release"

"Build"
  ==> "NUnit"
  ==> "xUnit"
  ==> "MsTest"
  ==> "RunTests"

"All"
  ==> "NuGet"
  ==> "Release"

"CleanDocs"
  ==> "GenerateDocs"
  ==> "KeepRunning"

"GenerateDocs"
  ==> "ReleaseDocs"

Target.runOrDefault "All"
