//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0011"

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

///////////////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
///////////////////////////////////////////////////////////////////////////////
var artifactsDir = "./artifacts/";
var localPackagesDir = "../LocalPackages";
GitVersion gitVersionInfo;
string nugetVersion;

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup(context =>
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });

    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(gitVersionInfo.NuGetVersion);

    nugetVersion = gitVersionInfo.NuGetVersion;

    Information("Building Nevermore v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
});

Teardown(context =>
{
    Information("Finished running tasks.");
});

//////////////////////////////////////////////////////////////////////
//  PRIVATE TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() => {
		CleanDirectory(artifactsDir);
		CleanDirectories("./source/**/bin");
		CleanDirectories("./source/**/obj");
		CleanDirectories("./source/**/TestResults");
	});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		DotNetCoreRestore("source");
    });


Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		 DotNetCoreBuild("./source", new DotNetCoreBuildSettings
		{
			Configuration = configuration,
			ArgumentCustomization = args => args.Append($"/p:Version={nugetVersion}")
		});
	});

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
		var projects = GetFiles("./source/**/*Tests.csproj");
		foreach(var project in projects)
			DotNetCoreTest(project.FullPath, new DotNetCoreTestSettings
			{
				Configuration = configuration,
				NoBuild = true
			});
	});
	
Task("CopyToArtifacts")
    .IsDependentOn("Test")
    .Does(() => {
		CreateDirectory(artifactsDir);
		CopyFiles($"./source/**/Nevermore*.nupkg", artifactsDir);
	});

Task("CopyToLocalPackages")
    .IsDependentOn("CopyToArtifacts")
    .WithCriteria(BuildSystem.IsLocalBuild)
    .Does(() => {
		CreateDirectory(localPackagesDir);
		CopyFiles($"{artifactsDir}*.nupkg", localPackagesDir);
	});

	
Task("Publish")
    .IsDependentOn("CopyToArtifacts")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .Does(() =>
{
    
	NuGetPush($"{artifactsDir}Nevermore.Contracts.{nugetVersion}.nupkg", new NuGetPushSettings {
		Source = "https://f.feedz.io/octopus-deploy/dependencies/nuget",
		ApiKey = EnvironmentVariable("FeedzIoApiKey")
	});

	NuGetPush($"{artifactsDir}Nevermore.{nugetVersion}.nupkg", new NuGetPushSettings {
		Source = "https://f.feedz.io/octopus-deploy/dependencies/nuget",
		ApiKey = EnvironmentVariable("FeedzIoApiKey")
	});
	
    if (gitVersionInfo.PreReleaseTag == "")
    {
        NuGetPush($"{artifactsDir}Nevermore.Contracts.{nugetVersion}.nupkg", new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NuGetApiKey")
        });

          NuGetPush($"{artifactsDir}Nevermore.{nugetVersion}.nupkg", new NuGetPushSettings {
            Source = "https://www.nuget.org/api/v2/package",
            ApiKey = EnvironmentVariable("NuGetApiKey")
        });
    }
	
});


Task("Default")
    .IsDependentOn("Publish")
    .IsDependentOn("CopyToLocalPackages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);
