using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using ReferenceModels;

namespace ReferenceProtector.Tasks;

/// <summary>
/// A task to collect all references in the project.
/// </summary>
public class CollectAllReferences : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// The output file to save the collected references to.
    /// </summary>
    [Required]
    public string? OutputFile { get; set; }

    /// <summary>
    /// The MSBuild project file to collect references from.
    /// This is typically the project file that contains the references to be collected.
    /// </summary>
    [Required]
    public string? MsBuildProjectFile { get; set; }

    /// <summary>
    /// The project references to collect.
    /// This is a collection of project references that will be processed to collect their assembly paths and link types.
    /// Each project reference should have the `OriginalProjectReferenceItemSpec` metadata set to the original project file path.
    /// This metadata is used to determine the original project reference item specification.
    /// </summary>
    public ITaskItem[] ProjectReferences { get; set; } = [];

    /// <inheritdoc />
    public override bool Execute()
    {
        if (MsBuildProjectFile is null)
        {
            Log.LogError("MsBuildProjectFile must be provided.");
            return false;
        }

        List<ReferenceItem> references = new List<ReferenceItem>();
        foreach (var projectReference in ProjectReferences)
        {
            var projectReferenceAssemblyPath = Path.GetFullPath(projectReference.ItemSpec);
            var referenceProjectFile = projectReference.GetMetadata("OriginalProjectReferenceItemSpec");

            // Weirdly, NuGet restore is actually how transitive project references are determined and they're
            // added to to project.assets.json and collected via the IncludeTransitiveProjectReferences target.
            // This also adds the NuGetPackageId metadata, so use that as a signal that it's transitive.
            bool isTransitiveDependency = !string.IsNullOrEmpty(projectReference.GetMetadata("NuGetPackageId"));

            references.Add(new ReferenceItem(
                Source: MsBuildProjectFile,
                Target: projectReferenceAssemblyPath,
                LinkType: isTransitiveDependency ? ReferenceKind.ProjectReferenceTransitive : ReferenceKind.ProjectReferenceDirect));
        }

        if (OutputFile is not null)
        {
            references.SaveToFile(OutputFile);
            return true;
        }

        return false;
    }
}