namespace ReferenceModels;

internal record ReferenceItem(string Source, string Target, ReferenceKind LinkType);

internal enum ReferenceKind
{
    ProjectReferenceDirect,
    ProjectReferenceTransitive,
}
