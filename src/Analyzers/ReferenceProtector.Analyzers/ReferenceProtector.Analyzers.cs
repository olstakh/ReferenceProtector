using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text.Json;

namespace ReferenceProtector.Analyzers;

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DiagnosticDescriptors;
using ReferenceProtector.Analyzers.Models;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReferenceProtectorAnalyzer : DiagnosticAnalyzer
{
    // TODO: Make it configurable via MSBuild properties or options
    internal const string DependencyRulesFile = "DependencyRules.json";

    internal const string DeclaredReferencesFile = "references.tsv";

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        IncludeFields = true,
        Converters = {
            new JsonStringEnumConverter<LinkType>(),
            new JsonStringEnumConverter<Policy>()
        }        
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        Descriptors.DependencyRulesNotProvided,
        Descriptors.InvalidDependencyRulesFormat,
        Descriptors.NoDependencyRulesMatchedCurrentProject];

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

        var content = dependencyRulesFile?.GetText(context.CancellationToken)?.ToString();

        if (content == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.DependencyRulesNotProvided, Location.None));
            return;
        }

        DependencyRules? rules = null;

        try
        {
            rules = JsonSerializer.Deserialize<DependencyRules>(content, s_jsonSerializerOptions);
        }
        catch (JsonException ex)
        {
        }

        if (rules == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidDependencyRulesFormat,
                Location.None,
                $"Invalid JSON format in {DependencyRulesFile}"));
            return;
        }

        if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue($"build_property.MSBuildProjectFullPath", out var projectPath))
        {
            return;
        }

        var thisProjectDependencyRules = (rules.ProjectDependencies ?? [])
            .Where(pd => IsMatchByName(pd.From, projectPath))
            .ToList();

        if (thisProjectDependencyRules.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.NoDependencyRulesMatchedCurrentProject,
                Location.None,
                projectPath));
            return;
        }

        var declaredReferences = context.Options.AdditionalFiles
            .FirstOrDefault(file => file.Path.EndsWith(DeclaredReferencesFile, StringComparison.OrdinalIgnoreCase));

        if (declaredReferences == null)
        {
            // Diagnostic?
            return;
        }

        // Analyze the content of the dependency rules file
    }
    
    private static bool IsMatchByName(string pattern, string project)
    {
        var regex = Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var match = Regex.IsMatch(project, regex, RegexOptions.IgnoreCase);
        return match;
    }    
}
