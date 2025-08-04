using Microsoft.CodeAnalysis;

namespace ReferenceProtector.Analyzers.DiagnosticDescriptors;

#pragma warning disable RS1037 // Add "CompilationEnd" custom tag to the diagnostic descriptor used to initialize field 'xxx' as it is used to report a compilation end diagnostic

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

    public static readonly DiagnosticDescriptor ProjectReferenceViolation = new(
        id: "RP0004",
        title: "Project reference violation",
        messageFormat: "Project reference '{0}' ==> '{1}' violates dependency rule '{2}' or one of its exceptions",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}