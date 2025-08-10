using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ReferenceProtector.Analyzers.Tests;

/// <summary>
/// Reference Protector analyzer tests.
/// These tests verify that the analyzer correctly identifies issues with project references based on defined dependency rules.
/// </summary>
public class ReferenceProtectorAnalyzerTests
{
    /// <summary>
    /// Verifies that the analyzer reports a diagnostic when no dependency rules file is provided.
    /// </summary>
    [Fact]
    public async Task NoDependencyRulesFile_ShouldReportDiagnostic_Async()
    {
        var test = GetAnalyzer();

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("RP0001")
                .WithNoLocation()
                .WithMessage("Provide DependencyRulesFile property to specify valid dependency rules file. Current path: DependencyRules.json."));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer reports a diagnostic when the dependency rules file is invalid.
    /// </summary>
    [Fact]
    public async Task InvalidDependencyRulesFile_ShouldReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", "Invalid JSON content")); // Invalid rules file

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("RP0002")
                .WithNoLocation()
                .WithMessage("Make sure the dependency rules file 'DependencyRules.json' is in the correct json format"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer reports a diagnostic when no dependency rules match the current project.
    /// </summary>
    [Fact]
    public async Task DependencyRulesFileProvided_ShouldReportIfNoMatches_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", "{}")); // Empty rules file

        test.ExpectedDiagnostics.Add(new DiagnosticResult("RP0003", Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
            .WithNoLocation()
            .WithMessage("No dependency rules matched the current project 'TestProject.csproj'"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer reports a diagnostic when a project reference violates a defined dependency rule.
    /// </summary>
    [Fact]
    public async Task ValidDependencyRulesFile_DependencyViolated_ShouldReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "ProjectDependencies": [
                    {
                        "From": "TestProject.csproj",
                        "To": "ReferencedProject.csproj",
                        "Description": "Can't reference this project directly",
                        "Policy": "Forbidden",
                        "LinkType": "Direct"
                    },
                    {
                        "From": "TestProject.csproj",
                        "To": "TransitiveReferencedProject.csproj",
                        "Description": "Can't reference this project transitively",
                        "Policy": "Forbidden",
                        "LinkType": "Transitive"
                    },
                ]
            }
            """));

        test.TestState.AdditionalFiles.Add(
            ("references.tsv", """
            TestProject.csproj	ProjectReferenceDirect	ReferencedProject.csproj
            TestProject.csproj	ProjectReferenceTransitive	TransitiveReferencedProject.csproj
            """));

        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0004")
            .WithNoLocation()
            .WithMessage("Project reference 'TestProject.csproj' ==> 'ReferencedProject.csproj' violates dependency rule 'Can't reference this project directly' or one of its exceptions. Please remove the dependency or update 'DependencyRules.json' file to allow it."));

        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0004")
            .WithNoLocation()
            .WithMessage("Project reference 'TestProject.csproj' ==> 'TransitiveReferencedProject.csproj' violates dependency rule 'Can't reference this project transitively' or one of its exceptions. Please remove the dependency or update 'DependencyRules.json' file to allow it."));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer does not report a diagnostic when a project reference matches an exception in the dependency rules.
    /// </summary>
    [Fact]
    public async Task ValidDependencyRulesFile_DependencyViolated_ExceptionsMatched_ShouldNotReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "ProjectDependencies": [
                    {
                        "From": "*",
                        "To": "*",
                        "Description": "Can't reference this project directly",
                        "Policy": "Forbidden",
                        "LinkType": "DirectOrTransitive",
                        "Exceptions": [
                            {
                                "From": "TestProject.csproj",
                                "To": "ReferencedProject.csproj",
                                "Justification": "This is an exception"
                            },
                            {
                                "From": "TestProject.csproj",
                                "To": "TransitiveReferencedProject.csproj",
                                "Justification": "This is an exception"
                            }
                        ]
                    }
                ]
            }
            """));

        test.TestState.AdditionalFiles.Add(
            ("references.tsv", """
            TestProject.csproj	ProjectReferenceDirect	ReferencedProject.csproj
            TestProject.csproj	ProjectReferenceTransitive	TransitiveReferencedProject.csproj
            """));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer does not report a diagnostic when the dependency rules file is valid and no declared references are present.
    /// </summary>
    [Fact]
    public async Task ValidDependencyRulesFile_NoDeclaredReferences_ShouldNotReportDiagnostics_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "ProjectDependencies": [
                    {
                        "From": "TestProject.csproj",
                        "To": "ReferencedProject",
                        "Description": "Test dependency",
                        "Policy": "Allowed",
                        "LinkType": "Direct"
                    }
                ]
            }
            """));

        // No diagnostics expected
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    private AnalyzerTest<DefaultVerifier> GetAnalyzer() =>
        new CSharpAnalyzerTest<ReferenceProtectorAnalyzer, DefaultVerifier>()
        {
            TestState =
            {
                Sources = { "class TestClass { }" },
                AnalyzerConfigFiles = { ("/.editorconfig", """
                    is_global=true
                    build_property.MSBuildProjectFullPath=TestProject.csproj
                    build_property.DependencyRulesFile=DependencyRules.json
                    build_property.EnableReferenceProtector=true
                    """) },
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };
}
