using Microsoft.CodeAnalysis;

namespace ReferenceProtector.Analyzers.DiagnosticDescriptors;

internal static class Descriptors
{
    public static readonly DiagnosticDescriptor DependencyRulesNotProvided = new(
        id: "RP0001",
        title: "Provide dependency rules for analysis",
        messageFormat: "",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidDependencyRulesFormat = new(
        id: "RP0002",
        title: "Invalid dependency rules format",
        messageFormat: "",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoDependencyRulesMatchedCurrentProject = new(
        id: "RP0003",
        title: "No dependency rules matched the current project",
        messageFormat: "No dependency rules matched the current project '{0}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);
}