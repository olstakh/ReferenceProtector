using System.Diagnostics;
using Xunit;

namespace ReferenceProtector.Tasks.IntegrationTests;

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
<Project Sdk="Microsoft.Build.Traversal">
    <ItemGroup>
        <ProjectReference Include="**\dirs.proj" />
        <ProjectReference Include="**\*.csproj" />
    </ItemGroup>
</Project>
""");
        }

        // Create the build props file
        {
            var buildPropsPath = Path.Combine(testDirectory, "Directory.Build.props");
            Output.WriteLine($"Creating build props file: {buildPropsPath}");

            File.WriteAllText(buildPropsPath, """
<Project>
  <Import Project="..\ReferenceProtector.props" />
</Project>
""");
        }

        // Create the build targets file
        {
            var buildTargetsPath = Path.Combine(testDirectory, "ReferenceProtector.Build.targets");
            Output.WriteLine($"Creating build targets file: {buildTargetsPath}");

            File.WriteAllText(Path.Combine(testDirectory, "Directory.Build.targets"), """
<Project>
  <Import Project="..\ReferenceProtector.targets" />
</Project>
""");
        }

        return testDirectory;
    }

    internal void CreateProject(string projectName)
    {
        var projectPath = Path.Combine(TestDirectory, projectName);

        // Ensure the project directory exists
        {
            Output.WriteLine($"Creating project directory: {projectPath}");
            Directory.CreateDirectory(projectPath);
        }

        // Create the project file
        {
            Output.WriteLine($"Creating project: {projectName}.cs at {projectPath}");

            File.WriteAllText(Path.Combine(projectPath, $"{projectName}.csproj"), """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>
""");
        }

        // Create a simple class file
        {
            Output.WriteLine($"Creating Class.cs in project: {projectName}");

            File.WriteAllText(Path.Combine(projectPath, $"Class.cs"), $@"
namespace {projectName};

public class Class1
{{
}}
");
        }
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

    internal async Task Build(string additionalArgs = "")
    {
        string buildArgs =
            $"build dirs.proj " +
            $"-m:1 -restore -nologo -nodeReuse:false -noAutoResponse " +
            $"/p:Configuration=Debug " +
            $"/p:ReferenceProtectorTaskAssembly={Path.Combine(Directory.GetCurrentDirectory(), "ReferenceProtector.Tasks.dll")} " +
            $"/v:m" +
            $" {additionalArgs}";

        await RunDotnetCommandAsync(TestDirectory, buildArgs, TestContext.Current.CancellationToken);
    }

    internal List<string> GetGeneratedReferencesFiles()
    {
        var files = Directory.GetFiles(TestDirectory, "_ReferenceProtector_DeclaredReferences.tsv", SearchOption.AllDirectories);
        return files.ToList();
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