using Microsoft.Build.Framework;

namespace ReferenceProtector.Tasks;

/// <summary>
/// A task to collect all references in the project.
/// </summary>
public class CollectAllReferences : Microsoft.Build.Utilities.Task
{
    [Required]
    public string? OutputFile { get; set; }

    [Output]
    public string OutputMessage { get; set; } = string.Empty;

    public override bool Execute()
    {
        OutputMessage = OutputFile ?? "N/A";
        return true;
    }
}