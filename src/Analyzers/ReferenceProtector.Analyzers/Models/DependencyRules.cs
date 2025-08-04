using System.Collections.Generic;

namespace ReferenceProtector.Analyzers.Models;

public record DependencyRules(
    IReadOnlyCollection<ProjectDependency> ProjectDependencies);

public record ProjectDependency(
    string From,
    string To,
    string Description,
    Policy Policy,
    LinkType LinkType,
    IReadOnlyCollection<Exceptions>? Exceptions);

public record Exceptions(
    string From,
    string To,
    string Justification);

public enum Policy
{
    Allowed,
    Forbidden
}

public enum LinkType
{
    Direct,
    Transitive,
}