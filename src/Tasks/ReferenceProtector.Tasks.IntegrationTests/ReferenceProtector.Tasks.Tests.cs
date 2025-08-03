using System.Diagnostics;
using System.Threading.Tasks;
using ReferenceProtector.Tasks.IntegrationTests;
using Xunit;

namespace ReferenceProtector.Tasks.Tests;

public class CollectAllReferencesIntegrationTests : TestBase
{
    public CollectAllReferencesIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task CollectAllReferences_ShouldCollectReferences()
    {
        CreateProject("TestProject1");
        CreateProject("TestProject2");
        CreateProject("TestProject3");
        await AddProjectReference("TestProject1", "TestProject2");
        await AddProjectReference("TestProject2", "TestProject3");
        await Build();

        var generatedFiles = GetGeneratedReferencesFiles();
        Assert.Equal(3, generatedFiles.Count);
    }
}