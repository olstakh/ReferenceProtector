using System.Diagnostics;
using System.Threading.Tasks;
using ReferenceModels;
using ReferenceProtector.Tasks.IntegrationTests;
using Xunit;

namespace ReferenceProtector.Tasks.Tests;

/// <summary>
/// Integration tests for the CollectAllReferences task.
/// These tests verify that the task correctly collects all project references and their links.
/// </summary>
public class CollectAllReferencesIntegrationTests : TestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectAllReferencesIntegrationTests"/> class.
    /// </summary>
    /// <param name="output">The output helper for logging test output.</param>
    public CollectAllReferencesIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// Verifies that the CollectAllReferences task generates the expected files.
    /// </summary>
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

    /// <summary>
    /// Verifies that the CollectAllReferences task correctly collects all project references and their links.
    /// </summary>
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