using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ReferenceProtector.Analyzers;

using DiagnosticDescriptors;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReferenceProtectorAnalyzer : DiagnosticAnalyzer
{
    // TODO: Make it configurable via MSBuild properties or options
    internal const string DependencyRulesFile = "DependencyRules.json";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.DependencyRulesNotProvided];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationAction(AnalyzeDependencyRules);
    }

    private void AnalyzeDependencyRules(CompilationAnalysisContext context)
    {
        var dependencyRulesFile = context.Options.AdditionalFiles
            .FirstOrDefault(file => file.Path.EndsWith(DependencyRulesFile, StringComparison.OrdinalIgnoreCase));

        if (dependencyRulesFile == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.DependencyRulesNotProvided, Location.None));
            return;
        }

        // Analyze the content of the dependency rules file
    }
}
