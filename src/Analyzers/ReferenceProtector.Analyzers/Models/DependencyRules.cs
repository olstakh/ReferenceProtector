using System.Collections.Generic;

namespace ReferenceProtector.Analyzers.Models;

internal record DependencyRules(
    IReadOnlyCollection<ProjectDependency> ProjectDependencies);

internal record ProjectDependency(
    string From,
    string To,
    string Description,
    Policy Policy,
    LinkType LinkType,
    IReadOnlyCollection<Exceptions>? Exceptions);

internal record Exceptions(
    string From,
    string To,
    string Justification);

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