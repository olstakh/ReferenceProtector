using System.Diagnostics;
using System.Threading.Tasks;
using ReferenceModels;
using ReferenceProtector.Tasks.IntegrationTests;
using Xunit;

namespace ReferenceProtector.Tasks.Tests;

public class CollectAllReferencesIntegrationTests : TestBase
{
    public CollectAllReferencesIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task CollectAllReferences_FilesAreGenerated_Async()
    {
        CreateProject("A");
        CreateProject("B");
        CreateProject("C");
        await Build();

        var generatedFiles = GetGeneratedReferencesFiles();
        Assert.Equal(3, generatedFiles.Count);

        var references = generatedFiles.SelectMany(file => ReferenceItemExtensions.LoadFromFile(file)).ToList();
        Assert.Empty(references);
    }

    [Fact]
    public async Task CollectAllReferences_LinksAreCorrect_Async()
    {
        CreateProject("A");
        CreateProject("B");
        CreateProject("C");
        await AddProjectReference("A", "B");
        await AddProjectReference("B", "C");
        await Build();

        var generatedFiles = GetGeneratedReferencesFiles();
        Assert.Equal(3, generatedFiles.Count);

        var references = generatedFiles.SelectMany(file => ReferenceItemExtensions.LoadFromFile(file))
            .OrderBy(x => (x.Source, x.Target, x.LinkType))
            .ToList();

        var expectedReferences = new List<ReferenceItem>
        {
            new ReferenceItem("A", "B", ReferenceKind.ProjectReferenceDirect),
            new ReferenceItem("B", "C", ReferenceKind.ProjectReferenceDirect),
            new ReferenceItem("A", "C", ReferenceKind.ProjectReferenceTransitive)
        }
        .Select(x => x with
        {
            Source = Path.Combine(TestDirectory, x.Source, $"{x.Source}.csproj"),
            Target = Path.Combine(TestDirectory, x.Target, $"{x.Target}.csproj"),
        })
        .OrderBy(x => (x.Source, x.Target, x.LinkType))
        .ToList();

        Assert.Equal(expectedReferences, references);
    }
}