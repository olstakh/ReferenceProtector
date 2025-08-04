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
}