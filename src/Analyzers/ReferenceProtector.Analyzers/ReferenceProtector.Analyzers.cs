using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Text.Json;

namespace ReferenceProtector.Analyzers;

using System.IO;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DiagnosticDescriptors;
using ReferenceModels;
using ReferenceProtector.Analyzers.Models;

/// <summary>
/// Analyzer for the Reference Protector project.
/// This analyzer checks project references against defined dependency rules.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ReferenceProtectorAnalyzer : DiagnosticAnalyzer
{
    // TODO: Make it configurable via MSBuild properties or options
    internal const string DeclaredReferencesFile = "_ReferenceProtector_DeclaredReferences.tsv";

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

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        Descriptors.DependencyRulesNotProvided,
        Descriptors.InvalidDependencyRulesFormat,
        Descriptors.NoDependencyRulesMatchedCurrentProject,
        Descriptors.ProjectReferenceViolation];

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationAction(AnalyzeDependencyRules);
    }

    private void AnalyzeDependencyRules(CompilationAnalysisContext context)
    {
        if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue($"build_property.EnableReferenceProtector", out var enableReferenceProtector) ||
            enableReferenceProtector.ToLower() != "true")
        {
            // The feature is disabled, no need to analyze
            return;
        }

        if (!context.Options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue($"build_property.DependencyRulesFile", out var dependencyRulesFileName) ||
            string.IsNullOrWhiteSpace(dependencyRulesFileName))
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.DependencyRulesNotProvided, Location.None, "N/A"));
            return;
        }

        var dependencyRulesFile = context.Options.AdditionalFiles
            .FirstOrDefault(file => Path.GetFullPath(file.Path).Equals(Path.GetFullPath(dependencyRulesFileName), StringComparison.OrdinalIgnoreCase));

        var content = dependencyRulesFile?.GetText(context.CancellationToken)?.ToString();

        if (content == null || dependencyRulesFile == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.DependencyRulesNotProvided, Location.None, properties: new Dictionary<string, string?>()
            {
            }.ToImmutableDictionary(),
            messageArgs: dependencyRulesFileName));
            return;
        }

        DependencyRules? rules = null;

        try
        {
            rules = JsonSerializer.Deserialize<DependencyRules>(content, s_jsonSerializerOptions);
        }
        catch (JsonException ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidDependencyRulesFormat,
                Location.None,
                properties: new Dictionary<string, string?>()
                {
                    { "Error", ex.Message }
                }.ToImmutableDictionary(),
                dependencyRulesFile.Path));
            return;            
        }

        if (rules == null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.InvalidDependencyRulesFormat,
                Location.None,
                dependencyRulesFile.Path));
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

        var declaredReferencesFile = context.Options.AdditionalFiles
            .FirstOrDefault(file => file.Path.EndsWith(DeclaredReferencesFile, StringComparison.OrdinalIgnoreCase));

        if (declaredReferencesFile == null)
        {
            // Diagnostic?
            return;
        }

        var declaredReferencesContent = declaredReferencesFile.GetText(context.CancellationToken)?.Lines;
        if (declaredReferencesContent == null)
        {
            return;
        }

        var declaredReferences = declaredReferencesContent
            .Where(line => !string.IsNullOrWhiteSpace(line.ToString()))
            .Select(line => ReferenceItem.FromLine(line.ToString()))
            .Where(r => IsMatchByName(r.Source, projectPath));

        AnalyzeDeclaredReferences(context, declaredReferences.ToImmutableArray(), thisProjectDependencyRules.ToImmutableArray(), dependencyRulesFile.Path);
    }

    private void AnalyzeDeclaredReferences(
        CompilationAnalysisContext context,
        ImmutableArray<ReferenceItem> declaredReferences,
        ImmutableArray<ProjectDependency> dependencyRules,
        string dependencyRulesFile)
    {
        foreach (var reference in declaredReferences)
        {
            var matchingRules = dependencyRules
                .Where(rule => IsMatchByName(rule.From, reference.Source) && IsMatchByName(rule.To, reference.Target))
                .Where(rule => rule.LinkType != LinkType.Transitive && reference.LinkType == ReferenceKind.ProjectReferenceDirect ||
                               rule.LinkType != LinkType.Direct && reference.LinkType == ReferenceKind.ProjectReferenceTransitive)
                .ToList();

            // Process the matching rules
            foreach (var rule in matchingRules)
            {
                // Apply the rule logic here
                var anyExceptionsMatched = rule.Exceptions?.Any(exception =>
                    IsMatchByName(exception.From, reference.Source) &&
                    IsMatchByName(exception.To, reference.Target)) ?? false;

                var referenceValid = rule.Policy switch
                {
                    Policy.Allowed when !anyExceptionsMatched => true,
                    Policy.Forbidden when anyExceptionsMatched => true,
                    _ => false
                };

                if (!referenceValid)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.ProjectReferenceViolation,
                        Location.None,
                        reference.Source,
                        reference.Target,
                        rule.Description,
                        dependencyRulesFile));
                }
            }
        }
    }
    
    private static bool IsMatchByName(string pattern, string project)
    {
        var regex = Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        var match = Regex.IsMatch(project, regex, RegexOptions.IgnoreCase);
        return match;
    }    
}
