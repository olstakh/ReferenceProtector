using System.Diagnostics;
using System.Text.RegularExpressions;
using Xunit;

namespace ReferenceProtector.IntegrationTests;

/// <summary>
/// Base class for integration tests.
/// </summary>
public class TestBase : IDisposable
{
    /// <summary>
    /// Output helper for logging test output.
    /// This is used to capture output during test execution, which can be useful for debugging.
    /// </summary>
    protected ITestOutputHelper Output { get; }

    /// <summary>
    /// Temporary directory for on-the-fly generated test projects.
    /// </summary>
    protected string TestDirectory { get; }

    private static readonly Regex WarningErrorRegex = new(
        @".+: (warning|error) (?<message>.+) \[(?<project>.+)\]",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture); 

    internal readonly record struct Warning(string Message, string Project, IEnumerable<string>? AltMessages = null);


    /// <summary>
    /// Initializes a new instance of the <see cref="TestBase"/> class.
    /// This constructor sets up the test environment by creating a unique directory for each test run.
    /// It also creates necessary files like `dirs.proj`, `Directory.Build.props`, and `Directory.Build.targets` in the test directory.
    /// The `ITestOutputHelper` is used to log messages during the test execution, which can be useful for debugging purposes.
    /// The test directory is created with a unique identifier to avoid conflicts between different test runs.
    /// </summary>
    /// <param name="output">The output helper for logging test output.</param>
    public TestBase(ITestOutputHelper output)
    {
        Output = output;
        TestDirectory = SetupTestEnvironment();
    }

    internal string SetupTestEnvironment()
    {
        var identifier = Guid.NewGuid();
        var testDirectory = Path.Combine(Directory.GetCurrentDirectory(), identifier.ToString());

        // Create the test directory
        {
            Output.WriteLine($"Creating test directory: {testDirectory}");
            Directory.CreateDirectory(testDirectory);
        }

        // Create the dirs.proj file
        {
            var dirsProjPath = Path.Combine(testDirectory, "dirs.proj");
            Output.WriteLine($"Creating dirs.proj file: {dirsProjPath}");

            File.WriteAllText(dirsProjPath, """
<?xml version="1.0" encoding="utf-8"?>
<Project DefaultTargets="Build">
    <ItemGroup>
        <ProjectFiles Include="**\*.csproj" />
    </ItemGroup>
    <Target Name="Restore">
        <MSBuild Projects="@(ProjectFiles)" Targets="Restore" />
    </Target>
    <Target Name="Build">
        <MSBuild Projects="@(ProjectFiles)" />
    </Target>
    <Target Name="Rebuild">
        <MSBuild Projects="@(ProjectFiles)" Targets="Rebuild" />
    </Target>
    <Target Name="Clean">
        <MSBuild Projects="@(ProjectFiles)" Targets="Clean" />
    </Target>
</Project>
""");
        }

        // Create the build props file
        {
            var buildPropsPath = Path.Combine(testDirectory, "Directory.Build.props");
            Output.WriteLine($"Creating build props file: {buildPropsPath}");

            File.WriteAllText(buildPropsPath, """
<Project>
  <ItemGroup>
    <PackageReference Include="ReferenceProtector" VersionOverride="*-*" />
  </ItemGroup>
</Project>

""");
        }

        // Create a nuget.config that includes both the local package source (for ReferenceProtector)
        // and NuGet.org (for framework packages like Microsoft.NETCore.App.Ref).
        // This avoids using -s which replaces all sources and can cause flaky NU1101 failures
        // when framework targeting packs aren't in the global NuGet cache yet.
        {
            var nugetConfigPath = Path.Combine(testDirectory, "nuget.config");
            var localSource = Directory.GetCurrentDirectory();
            Output.WriteLine($"Creating nuget.config file: {nugetConfigPath} (local source: {localSource})");

            File.WriteAllText(nugetConfigPath, $"""
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="Local" value="{localSource}" />
    <add key="NuGet.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
""");
        }

        return testDirectory;
    }

    internal string CreateProject(string projectName)
    {
        var projectFolder = Path.Combine(TestDirectory, projectName);
        var projectFilePath = Path.Combine(projectFolder, $"{projectName}.csproj");

        // Ensure the project directory exists
        {
            Output.WriteLine($"Creating project directory: {projectFolder}");
            Directory.CreateDirectory(projectFolder);
        }

        // Create the project file
        {
            Output.WriteLine($"Creating project: {projectName}.cs at {projectFolder}");

            File.WriteAllText(projectFilePath, """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
""");
        }

        // Create a simple class file
        {
            Output.WriteLine($"Creating Class.cs in project: {projectName}");

            File.WriteAllText(Path.Combine(projectFolder, $"Class.cs"), $@"
namespace {projectName};

public class Class1
{{
}}
");
        }

        return projectFilePath;
    }

    internal async Task AddProjectReference(string projectName, string referenceProjectName)
    {
        var projectPath = Path.Combine(TestDirectory, projectName, $"{projectName}.csproj");
        var referencePath = Path.Combine(TestDirectory, referenceProjectName, $"{referenceProjectName}.csproj");

        await RunDotnetCommandAsync(TestDirectory, $"add {projectPath} reference {referencePath}", TestContext.Current.CancellationToken);    
    }

    internal async Task AddPackageReference(string projectName, string packageName)
    {
        var projectPath = Path.Combine(TestDirectory, projectName, $"{projectName}.csproj");

        await RunDotnetCommandAsync(TestDirectory, $"add {projectPath} package {packageName}", TestContext.Current.CancellationToken);    
    }

    internal async Task<IReadOnlyList<Warning>> Build(string additionalArgs = "")
    {
        string logDirBase = Path.Combine(TestDirectory, "Logs");
        string binlogFilePath = Path.Combine(logDirBase, "build.binlog");
        string warningsFilePath = Path.Combine(logDirBase, "build.warnings.log");
        string errorsFilePath = Path.Combine(logDirBase, "build.errors.log");

        await RunDotnetCommandAsync(TestDirectory, $"restore dirs.proj -f", TestContext.Current.CancellationToken);
        await RunDotnetCommandAsync(TestDirectory, $"list dirs.proj package", TestContext.Current.CancellationToken);

        string buildArgs =
            $"build dirs.proj " +
            $"-m:1 -t:Rebuild -nologo -nodeReuse:false -noAutoResponse " +
            $"/p:Configuration=Debug " +
            $"-bl:\"{binlogFilePath}\" " +
            $"-flp1:logfile=\"{errorsFilePath}\";errorsonly " +
            $"-flp2:logfile=\"{warningsFilePath}\";warningsonly " +
            $"/v:m" +
            $" {additionalArgs}";

        await RunDotnetCommandAsync(TestDirectory, buildArgs, TestContext.Current.CancellationToken);

        List<Warning> actualWarnings = new();
        foreach (string line in await File.ReadAllLinesAsync(warningsFilePath))
        {
            Match match = WarningErrorRegex.Match(line);
            if (match.Success)
            {
                string message = match.Groups["message"].Value;
                string projectFullPath = match.Groups["project"].Value;
                string projectRelativePath = projectFullPath[(TestDirectory.Length + 1)..];

                // Normalize slashes for the project paths
                projectRelativePath = projectRelativePath.Replace('\\', '/');

                actualWarnings.Add(new Warning(message, projectRelativePath));
            }
        }

        return actualWarnings;
    }

    private async Task RunDotnetCommandAsync(string workingDirectory, string args, CancellationToken ct)
    {
        Output.WriteLine($"Running dotnet with arguments: {args}");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = args,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Assert.NotNull(process);
        Assert.False(process.HasExited);

        string output = await process.StandardOutput.ReadToEndAsync(ct);
        string error = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        Output.WriteLine($"MSBuild Output: {output}");
        Output.WriteLine($"MSBuild Error: {error}");

        Assert.Equal(0, process.ExitCode);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Output.WriteLine("Disposing CollectAllReferencesIntegrationTests...");

        // Clean up the test directory
        if (Directory.Exists(TestDirectory))
        {
            try
            {
                Directory.Delete(TestDirectory, true);
                Output.WriteLine($"Test Directory Deleted: {TestDirectory}");
            }
            catch (IOException ex)
            {
                Output.WriteLine($"Error deleting test directory: {ex.Message}");
            }
        }
    }
}