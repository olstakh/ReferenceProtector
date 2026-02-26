using System.Collections.Generic;

namespace ReferenceProtector.Analyzers.Models;

internal record DependencyRules(
    IReadOnlyCollection<ProjectDependency> ProjectDependencies,
    IReadOnlyCollection<PackageDependency> PackageDependencies);

internal record ProjectDependency(
    string From,
    string To,
    string Description,
    Policy Policy,
    LinkType LinkType,
    IReadOnlyCollection<Exceptions>? Exceptions);

// Only direct package dependencies are considered for now, so LinkType is omitted
internal record PackageDependency(
    string From,
    string To,
    string Description,
    Policy Policy,
    IReadOnlyCollection<Exceptions>? Exceptions);

internal record Exceptions(
    string From,
    string To,
    string Justification,
    bool IsTechDebt = false);

internal enum Policy
{
    Allowed,
    Forbidden
}

internal enum LinkType
{
    Direct,
    Transitive,
    DirectOrTransitive,
}