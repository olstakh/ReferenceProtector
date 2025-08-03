using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace ReferenceProtector.Tasks.Tests;

public class CollectAllReferencesIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDirectory;

    public CollectAllReferencesIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        _testDirectory = SetupTestEnvironment();
        _output.WriteLine($"Test Directory Created: {_testDirectory}");
    }

    internal static string SetupTestEnvironment()
    {
        var identifier = Guid.NewGuid();
        var testDirectory = Path.Combine(Directory.GetCurrentDirectory(), identifier.ToString());
        Directory.CreateDirectory(testDirectory);

        File.WriteAllText(Path.Combine(testDirectory, "dirs.proj"), """
<?xml version="1.0" encoding="utf-8"?>
<Project Sdk="Microsoft.Build.Traversal">
    <ItemGroup>
        <ProjectReference Include="**\dirs.proj" />
        <ProjectReference Include="**\*.csproj" />
    </ItemGroup>
</Project>
""");

        File.WriteAllText(Path.Combine(testDirectory, "Directory.Build.props"), """
<Project>
  <Import Project="..\ReferenceProtector.Build.props" />
</Project>
""");

        File.WriteAllText(Path.Combine(testDirectory, "Directory.Build.targets"), """
<Project>
  <Import Project="..\ReferenceProtector.Build.targets" />
</Project>
""");

        return testDirectory;
    }

    internal static void CreateProject(string testDirectory, string projectName)
    {
        var projectPath = Path.Combine(testDirectory, projectName);
        Directory.CreateDirectory(projectPath);
        File.WriteAllText(Path.Combine(projectPath, $"{projectName}.csproj"), """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
""");

        File.WriteAllText(Path.Combine(projectPath, $"Class.cs"), $@"
namespace {projectName};

public class Class1
{{
}}
");
    }

    internal static async Task AddProjectReference(string testDirectory, string projectName, string referenceProjectName, ITestOutputHelper logger)
    {
        var projectPath = Path.Combine(testDirectory, projectName, $"{projectName}.csproj");
        var referencePath = Path.Combine(testDirectory, referenceProjectName, $"{referenceProjectName}.csproj");

        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "add " + projectPath + " reference " + referencePath,
            WorkingDirectory = testDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Assert.NotNull(process);
        Assert.False(process.HasExited);

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        logger.WriteLine($"MSBuild Output: {output}");
        logger.WriteLine($"MSBuild Error: {error}");
    }

    [Fact]
    public async Task CollectAllReferences_ShouldCollectReferences()
    {
        CreateProject(_testDirectory, "TestProject1");
        CreateProject(_testDirectory, "TestProject2");
        CreateProject(_testDirectory, "TestProject3");
        await AddProjectReference(_testDirectory, "TestProject1", "TestProject2", _output);
        await AddProjectReference(_testDirectory, "TestProject2", "TestProject3", _output);
        await RunMSBuildAsync(_testDirectory, _output);
    }

    public void Dispose()
    {
        _output.WriteLine("Disposing CollectAllReferencesIntegrationTests...");

        // Clean up the test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
            _output.WriteLine($"Test Directory Deleted: {_testDirectory}");
        }
    }

    internal static async Task RunMSBuildAsync(string testDirectory, ITestOutputHelper logger)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        logger.WriteLine($"Current Directory: {currentDirectory}");

        string buildArgs =
            $"build dirs.proj " +
            $"-m:1 -t:Rebuild -restore -nologo -nodeReuse:false -noAutoResponse " +
            $"/p:Configuration=Debug " +
            $"/p:ReferenceProtectorTaskAssembly={Path.Combine(Directory.GetCurrentDirectory(), "ReferenceProtector.Tasks.dll")} " +
            $"/v:m";

        logger.WriteLine($"Running MSBuild with arguments: {buildArgs}");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = buildArgs,
            WorkingDirectory = testDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Assert.NotNull(process);
        Assert.False(process.HasExited);

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        logger.WriteLine($"MSBuild Output: {output}");
        logger.WriteLine($"MSBuild Error: {error}");

        Assert.Equal(0, process.ExitCode);
    }
}