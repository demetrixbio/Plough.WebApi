#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
#r "Facades/netstandard"
#r "netstandard"
#endif
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.DotNet
open Fake.Tools
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Api
open Fake.BuildServer
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators

let rootDir = __SOURCE_DIRECTORY__ 
let gitOwner = "demetrixbio"
let gitRepoName = "Plough.WebApi"


let sln = rootDir </> "Plough.WebApi.sln"

let srcGlob = rootDir </> "src/**/*.??proj"
let testsGlob = rootDir </> "test/**/*.??proj"


let distDir = rootDir </> "dist"
let distGlob = distDir </> "*.nupkg"

let nugetPublishUrl = "https://www.nuget.org"



let githubToken = Environment.environVarOrNone "GITHUB_TOKEN"
Option.iter(TraceSecrets.register "<GITHUB_TOKEN>" ) githubToken

let nugetToken = Environment.environVarOrNone "NUGET_TOKEN"
Option.iter(TraceSecrets.register "<NUGET_TOKEN>") nugetToken


let isRelease (targets : Target list) =
    targets
    |> Seq.map(fun t -> t.Name)
    |> Seq.exists ((=)"Release")

let configuration (targets : Target list) =
    let defaultVal = if isRelease targets then "Release" else "Debug"
    match Environment.environVarOrDefault "CONFIGURATION" defaultVal with
    | "Debug" -> DotNet.BuildConfiguration.Debug
    | "Release" -> DotNet.BuildConfiguration.Release
    | config -> DotNet.BuildConfiguration.Custom config

Target.initEnvironment ()


Target.create "Clean" (fun _ ->
    [distDir]
    |> Shell.cleanDirs

    !! srcGlob
    ++ testsGlob
    |> Seq.collect(fun p ->
        ["bin";"obj"]
        |> Seq.map(fun sp -> System.IO.Path.GetDirectoryName p </> sp ))
    |> Shell.cleanDirs

    [
        rootDir </> "paket-files/paket.restore.cached"
    ]
    |> Seq.iter Shell.rm
)


Target.create "Restore" (fun _ ->
    [sln]
    |> Seq.iter (DotNet.restore id)
)

Target.create "Build" (fun _ ->
    [sln]
    |> Seq.iter (DotNet.build id)
)

Target.create "Test" (fun _ ->
  [sln]
  |> Seq.iter (DotNet.test id)
)
let releaseNotesFilePath = "RELEASE_NOTES.md"
let release = lazy(ReleaseNotes.load releaseNotesFilePath)
let releaseVersion = lazy(release.Value.NugetVersion)
let releaseNotes = lazy(String.concat "\n" release.Value.Notes)
// PUBLISH TO NUGET

Target.create "Pack" (fun ctx ->
   

    let args =
        [
            sprintf "/p:PackageVersion=%s" releaseVersion.Value
            sprintf "/p:PackageReleaseNotes=\"%s\"" releaseNotes.Value
        ]
    DotNet.pack (fun c ->
        { c with
            Configuration = configuration (ctx.Context.AllExecutingTargets)
            OutputPath = Some distDir
            Common =
                c.Common
                |> DotNet.Options.withAdditionalArgs args
        }) sln


)

Target.create "PublishToNuGet" (fun _ ->
    Paket.push(fun p ->
        { p with
            ToolType = ToolType.CreateLocalTool()
            PublishUrl = nugetPublishUrl
            WorkingDir = distDir
            ApiKey =
              match nugetToken with
              | Some s -> s
              | _ -> p.ApiKey // assume paket-config was set properly
        }
    )
)
let remote = Environment.environVarOrDefault "PWA_GIT_REMOTE" "origin"
let tagFromVersionNumber versionNumber = sprintf "v%s" versionNumber
Target.create "GitRelease" (fun _ ->
    Git.Staging.stageFile "" releaseNotesFilePath
        |> ignore
    !! "src/**/AssemblyInfo.fs"
        |> Seq.iter (Git.Staging.stageFile "" >> ignore)

    Git.Commit.exec "" (sprintf "VER: Bump version to %s\n\n%s" releaseVersion.Value releaseNotes.Value)
    Git.Branches.pushBranch "" remote ""

    let tag = tagFromVersionNumber releaseVersion.Value

    Git.Branches.tag "" tag
    Git.Branches.pushTag "" remote tag
)
Target.create "GitHubRelease" (fun _ ->
    let token =
        match githubToken with
        | Some s -> s
        | _ -> failwith "please set the github_token environment variable to a github personal access token with repo access."

    let files = !! distGlob

    GitHub.createClientWithToken token
    |> GitHub.draftNewRelease gitOwner gitRepoName (tagFromVersionNumber releaseVersion.Value) (release.Value.SemVer.PreRelease <> None) [releaseNotes.Value]
    |> GitHub.uploadFiles files
    |> GitHub.publishDraft
    |> Async.RunSynchronously
)

Target.create "All" ignore
Target.create "Release" ignore

"Clean"
  ==> "Restore"
  ==> "Build"
  ==> "Test"
  ==> "Pack"
  ==> "All"

"Pack"
  ==> "PublishToNuGet"
  ==> "GitRelease"
  ==> "GitHubRelease"
  ==> "Release"

Target.runOrDefaultWithArguments "All"
