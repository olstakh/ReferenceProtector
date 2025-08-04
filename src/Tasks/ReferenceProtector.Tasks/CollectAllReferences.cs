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
    [Required]
    public string? OutputFile { get; set; }

    [Required]
    public string? MsBuildProjectFile { get; set; }

    public ITaskItem[] ProjectReferences { get; set; } = [];

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