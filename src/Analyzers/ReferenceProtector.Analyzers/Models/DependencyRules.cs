using System.Collections.Generic;

namespace ReferenceProtector.Analyzers.Models;

internal record DependencyRules<T>(
    IReadOnlyCollection<ProjectDependency<T>> ProjectDependencies,
    IReadOnlyCollection<PackageDependency<T>> PackageDependencies);

internal record ProjectDependency<T>(
    T From,
    T To,
    string Description,
    Policy Policy,
    LinkType LinkType,
    IReadOnlyCollection<Exceptions<T>>? Exceptions)
{
    public ProjectDependency<TOut> Convert<TOut>(Converter<T, TOut> converter) =>
        new ProjectDependency<TOut>(
            converter(From),
            converter(To),
            Description,
            Policy,
            LinkType,
            Exceptions?.Select(e => e.Convert(converter)).ToArray());
}

// Only direct package dependencies are considered for now, so LinkType is omitted
internal record PackageDependency<T>(
    T From,
    T To,
    string Description,
    Policy Policy,
    IReadOnlyCollection<Exceptions<T>>? Exceptions)
{
    public PackageDependency<TOut> Convert<TOut>(Converter<T, TOut> converter) =>
        new PackageDependency<TOut>(
            converter(From),
            converter(To),
            Description,
            Policy,
            Exceptions?.Select(e => e.Convert(converter)).ToArray());
}

internal record Exceptions<T>(
    T From,
    T To,
    string Justification)
{
    public Exceptions<TOut> Convert<TOut>(Converter<T, TOut> converter) =>
        new Exceptions<TOut>(converter(From), converter(To), Justification);
}

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