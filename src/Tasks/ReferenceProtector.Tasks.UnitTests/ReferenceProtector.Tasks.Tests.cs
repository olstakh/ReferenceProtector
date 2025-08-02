using Xunit;

namespace ReferenceProtector.Tasks.Tests;

public class CollectAllReferencesTests
{
    [Fact]
    public void Execute_ShouldSetOutputMessage_WhenOutputFileIsProvided()
    {
        // Arrange
        var task = new CollectAllReferences
        {
            OutputFile = "references.txt"
        };

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.Equal("references.txt", task.OutputMessage);
    }

    [Fact]
    public void Execute_ShouldSetOutputMessageToN_A_WhenOutputFileIsNull()
    {
        // Arrange
        var task = new CollectAllReferences();

        // Act
        var result = task.Execute();

        // Assert
        Assert.True(result);
        Assert.Equal("N/A", task.OutputMessage);
    }
}