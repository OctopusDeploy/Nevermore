using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.OctoVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.IO.FileSystemTasks;

class BuildNevermore : NukeBuild
{
    const string CiBranchNameEnvVariable = "OCTOVERSION_CurrentBranch";

    [Parameter("Branch name for OctoVersion to use to calculate the version number. Can be set via the environment variable " + CiBranchNameEnvVariable + ".", Name = CiBranchNameEnvVariable)]
    string BranchName { get; set; }

    [Parameter("Whether to auto-detect the branch name - this is okay for a local build, but should not be used under CI.")]
    readonly bool AutoDetectBranch = IsLocalBuild;

    [Parameter] readonly string Configuration = "Release";

    AbsolutePath LocalPackagesDir => RootDirectory / ".." / "LocalPackages";

    [OctoVersion(BranchMember = nameof(BranchName), AutoDetectBranchMember = nameof(AutoDetectBranch))]
    public OctoVersionInfo OctoVersionInfo;

    [Solution(GenerateProjects = true)] public Solution Solution;

    AbsolutePath SourceDirectory => Solution.Directory;
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    public static int Main() => Execute<BuildNevermore>(x => x.Default);

    Target Clean => _ => _
        .Executes(() =>
    {
        ArtifactsDirectory.CreateOrCleanDirectory();
        SourceDirectory.GlobDirectories("**/bin").ForEach(x => x.CreateOrCleanDirectory());
        SourceDirectory.GlobDirectories("**/obj").ForEach(x => x.CreateOrCleanDirectory());
        SourceDirectory.GlobDirectories("**/TestResults").ForEach(x => x.CreateOrCleanDirectory());
    });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
    {
        DotNetRestore(_ => _
            .SetProjectFile(Solution));
    });

    Target Build => _ => _
        .DependsOn(Restore)
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(OctoVersionInfo.MajorMinorPatch)
                .SetFileVersion(OctoVersionInfo.MajorMinorPatch)
                .SetInformationalVersion(OctoVersionInfo.InformationalVersion)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Build)
        .Executes(() =>
    {
        var projects = SourceDirectory.GlobFiles("**/*Tests.csproj");
        foreach (var project in projects)
            DotNetTest(_ => _
            .SetProjectFile(project)
            .SetConfiguration(Configuration)
            .SetNoBuild(true)
            .SetLoggers("trx")
            );
    });

    Target CopyToArtifacts => _ => _
        .DependsOn(Test)
        .DependsOn(Pack)
        .Executes(() =>
    {
        ArtifactsDirectory.CreateDirectory();
        SourceDirectory.GlobFiles("**/Nevermore*.nupkg")
            .ForEach(f => CopyFileToDirectory(f, ArtifactsDirectory, FileExistsPolicy.Overwrite));
        SourceDirectory.GlobFiles("**/Nevermore*.snupkg")
            .ForEach(f => CopyFileToDirectory(f, ArtifactsDirectory, FileExistsPolicy.Overwrite));
    });

    Target CopyToLocalPackages => _ => _
        .DependsOn(CopyToArtifacts)
        .DependsOn(Pack)
        .OnlyWhenStatic(() => IsLocalBuild)
        .Executes(() =>
    {
        LocalPackagesDir.CreateOrCleanDirectory();
        ArtifactsDirectory.GlobFiles("*.nupkg")
            .ForEach(f => CopyFileToDirectory(f, LocalPackagesDir, FileExistsPolicy.Overwrite));
        ArtifactsDirectory.GlobFiles("*.snupkg")
            .ForEach(f => CopyFileToDirectory(f, LocalPackagesDir, FileExistsPolicy.Overwrite));
    });

    Target Pack => _ => _
        .DependsOn(Test)
        .Executes(() =>
        {
            DotNetPack(_ => _
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoBuild()
                .EnableNoRestore()
                .EnableIncludeSymbols()
                .AddProperty("Version", OctoVersionInfo.FullSemVer));
        });

    Target Default => _ => _
        .DependsOn(CopyToLocalPackages);
}