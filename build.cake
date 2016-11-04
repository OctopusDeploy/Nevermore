//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=3.6.4"
#addin "MagicChunks"

using Path = System.IO.Path;
using IO = System.IO;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts";
var projectsToPackage = new[] {
    "./source/Nevermore.Contracts",
    "./source/Nevermore"
};
var cleanups = new List<Action>();
var isContinuousIntegrationBuild = !BuildSystem.IsLocalBuild;

var gitVersionInfo = GitVersion(new GitVersionSettings {
    OutputType = GitVersionOutput.Json
});

var nugetVersion = gitVersionInfo.NuGetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    Information("Building Nevermore v{0}", nugetVersion);
     if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);
    if(BuildSystem.IsRunningOnAppVeyor)
        AppVeyor.UpdateBuildVersion(gitVersionInfo.NuGetVersion);
});

Teardown(context =>
{
    Information("Cleaning up");
    foreach(var item in cleanups)
        item();

    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("__Clean")
    .Does(() =>
{
    CleanDirectory(artifactsDir);
    CleanDirectories("./source/**/bin");
    CleanDirectories("./source/**/obj");
});

Task("__Restore")
    .Does(() => DotNetCoreRestore());

Task("__UpdateProjectJsonVersion")
    .Does(() =>
{
    foreach(var project in projectsToPackage)
    {
        var projectJson = $"{project}/project.json";
        RestoreFileOnCleanup(projectJson);
        Information("Updating {0} version -> {1}", projectJson, nugetVersion);

        TransformConfig(projectJson, projectJson, new TransformationCollection {
            { "version", nugetVersion }
        });
    }
});

Task("__Build")
    .IsDependentOn("__Clean")
    .IsDependentOn("__Restore")
    .IsDependentOn("__UpdateProjectJsonVersion")
    .Does(() =>
{
    DotNetCoreBuild("**/project.json", new DotNetCoreBuildSettings
    {
        Configuration = configuration
    });
});

Task("__Test")
    .IsDependentOn("__Build")
    .Does(() =>
{
    GetFiles("**/*Tests/project.json")
        .ToList()
        .ForEach(testProjectFile => 
        {
            DotNetCoreTest(testProjectFile.ToString(), new DotNetCoreTestSettings
            {
                Configuration = configuration,
                WorkingDirectory = Path.GetDirectoryName(testProjectFile.ToString())
            });
        });
});


Task("__Pack")
    .IsDependentOn("__Test")
    .Does(() => 
{
    foreach(var project in projectsToPackage)
        DotNetCorePack(project, new DotNetCorePackSettings
        {
            Configuration = configuration,
            OutputDirectory = artifactsDir,
            NoBuild = true
        });
});

Task("__Publish")
    .IsDependentOn("__Pack")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    
	NuGetPush($"{artifactsDir}/Nevermore.Contracts.{nugetVersion}.nupkg", new NuGetPushSettings {
		Source = "https://octopus.myget.org/F/octopus-dependencies/api/v3/index.json",
		ApiKey = EnvironmentVariable("MyGetApiKey")
	});

	NuGetPush($"{artifactsDir}/Nevermore.{nugetVersion}.nupkg", new NuGetPushSettings {
		Source = "https://octopus.myget.org/F/octopus-dependencies/api/v3/index.json",
		ApiKey = EnvironmentVariable("MyGetApiKey")
	});
	
    if (gitVersionInfo.PreReleaseTag == "")
    {
        NuGetPush($"{artifactsDir}/Nevermore.Contracts.{nugetVersion}.nupkg", new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NuGetApiKey")
        });

          NuGetPush($"{artifactsDir}/Nevermore.{nugetVersion}.nupkg", new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NuGetApiKey")
        });
    }
	
});

Task("__CopyToLocalPackages")
    .WithCriteria(BuildSystem.IsLocalBuild)
    .IsDependentOn("__Pack")
    .Does(() =>
{
    CreateDirectory("../LocalPackages");
    CopyFiles($"{artifactsDir}/*.{nugetVersion}*.nupkg", "../LocalPackages");
});


//////////////////////////////////////////////////////////////////////
// HELPERS
//////////////////////////////////////////////////////////////////////

private void RestoreFileOnCleanup(string file)
{
    var contents = System.IO.File.ReadAllBytes(file);
    cleanups.Add(() => {
        Information("Restoring {0}", file);
        System.IO.File.WriteAllBytes(file, contents);
    });
}


//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Default")
    .IsDependentOn("__CopyToLocalPackages")
    .IsDependentOn("__Publish");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
