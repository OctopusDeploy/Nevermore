//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0-beta0011"
#addin "Cake.FileHelpers"

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
        MSBuild("./source/Nevermore.sln", settings => 
			settings.SetVerbosity(Verbosity.Minimal)
					.UseToolVersion(MSBuildToolVersion.VS2017)
					.SetConfiguration("Release")
					.WithTarget("Restore"));
    });


Task("Build")
    .IsDependentOn("Restore")
    .IsDependentOn("Clean")
    .Does(() => {
		MSBuild("./source/Nevermore.sln", settings => 
			settings.SetVerbosity(Verbosity.Minimal)
					.UseToolVersion(MSBuildToolVersion.VS2017)
					.SetConfiguration("Release")
					.WithProperty("Version", nugetVersion)
					.WithTarget("Build"));
	});

Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
		var projects = GetFiles("./source/**/*Tests.csproj");
		foreach(var project in projects)
			DotNetCoreTest(project.FullPath, new DotNetCoreTestSettings
			{
				Configuration = configuration,
				NoBuild = true,
				ArgumentCustomization = args => args.Append("-l trx")
			});
	});
	
Task("CopyToArtifacts")
    .IsDependentOn("Test")
    .Does(() => {
		CreateDirectory(artifactsDir);
		CopyFiles($"./source/**/*.nupkg", artifactsDir);
	});

Task("CopyToLocalPackages")
    .IsDependentOn("CopyToArtifacts")
    .WithCriteria(BuildSystem.IsLocalBuild)
    .Does(() => {
		CreateDirectory(localPackagesDir);
		CopyFiles($"./source/**/*.nupkg", localPackagesDir);
	});

Task("Default")
    .IsDependentOn("CopyToLocalPackages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(target);