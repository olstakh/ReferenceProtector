using Xunit;
using Microsoft.Build.Framework;
using Moq;

namespace ReferenceProtector.Tasks.Tests;

public class CollectAllReferencesTests
{
    private readonly Mock<IBuildEngine> _mockBuildEngine;

    public CollectAllReferencesTests()
    {
        _mockBuildEngine = new Mock<IBuildEngine>(MockBehavior.Strict);
    }

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