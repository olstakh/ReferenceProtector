using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace ReferenceProtector.Tasks.Tests;

public class CollectAllReferencesIntegrationTests
{
    private readonly int _randomSeed = Environment.TickCount;
    private readonly ITestOutputHelper _output;
    private readonly Random _random;


    public CollectAllReferencesIntegrationTests(ITestOutputHelper output)
    {
        _output = output;

        _output.WriteLine($"Random Seed: {_randomSeed}");
        _random = new Random(_randomSeed);
    }

    [Fact]
    public async Task CollectAllReferences_ShouldCollectReferences()
    {
        _output.WriteLine("Starting CollectAllReferences integration test...");

        await RunMSBuildAsync();

        Assert.Fail("boo");
    }

    private async Task RunMSBuildAsync()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        _output.WriteLine($"Current Directory: {currentDirectory}");

        string buildArgs =
            $"build dirs.proj " +
            $"-m:1 -t:Rebuild -restore -nologo -nodeReuse:false -noAutoResponse " +
            $"/p:Configuration=Debug " +
            $"/p:ReferenceProtectorTaskAssembly={Path.Combine(Directory.GetCurrentDirectory(), "ReferenceProtector.Tasks.dll")} " +
            $"/v:m";

        _output.WriteLine($"Running MSBuild with arguments: {buildArgs}");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = buildArgs,
            WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Manual"),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        Assert.NotNull(process);

        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        _output.WriteLine($"MSBuild Output: {output}");
        _output.WriteLine($"MSBuild Error: {error}");
    }
}