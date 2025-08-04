using Xunit;
using Microsoft.Build.Framework;
using Moq;

namespace ReferenceProtector.Tasks.Tests;

/// <summary>
/// Tests for the CollectAllReferences MSBuild task.
/// </summary>
public class CollectAllReferencesTests
{
    private readonly Mock<IBuildEngine> _mockBuildEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollectAllReferencesTests"/> class.
    /// </summary>
    public CollectAllReferencesTests()
    {
        _mockBuildEngine = new Mock<IBuildEngine>(MockBehavior.Strict);
    }

    /// <summary>
    /// Verifies that the CollectAllReferences task executes successfully when provided with valid parameters.
    /// This test checks that the task can run without errors when the required properties are set correctly
    /// </summary>
    [Fact]
    public void Execute_ShouldSetOutputMessage_WhenOutputFileIsProvided()
    {
        // Arrange
        var task = new CollectAllReferences
        {
            OutputFile = "references.txt",
            BuildEngine = _mockBuildEngine.Object,
            MsBuildProjectFile = "TestProject.csproj",
        };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Verifies that the CollectAllReferences task fails when the OutputFile is null.
    /// This test checks that the task correctly handles the case where the OutputFile property is not set,
    /// which is a required parameter for the task to execute successfully.
    /// </summary>
    [Fact]
    public void Execute_ShouldSetOutputMessageToN_A_WhenOutputFileIsNull()
    {
        // Arrange
        var task = new CollectAllReferences()
        {
            BuildEngine = _mockBuildEngine.Object,
            MsBuildProjectFile = "TestProject.csproj",
        };

        // Act
        var result = task.Execute();

        // Assert
        Assert.False(result);
    }
}