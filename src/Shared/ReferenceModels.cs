namespace ReferenceModels;
using System;

internal record ReferenceItem(string Source, string Target, ReferenceKind LinkType)
{
    public static ReferenceItem FromLine(string line)
    {
        var parts = line.Split('\t');
        if (parts.Length != 3)
        {
            throw new FormatException($"Invalid reference item format: {line}");
        }

        return new ReferenceItem(
            Source: parts[0],
            LinkType: (ReferenceKind)Enum.Parse(typeof(ReferenceKind), parts[1]),
            Target: parts[2]);
    }    
}

internal enum ReferenceKind
{
    ProjectReferenceDirect,
    ProjectReferenceTransitive,
}
