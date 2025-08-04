using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ReferenceProtector.Analyzers.Tests;

public class ReferenceProtectorAnalyzerTests
{
    [Fact]
    public async Task NoDependencyRulesFile_ShouldReportDiagnostic_Async()
    {
        var test = GetAnalyzer();

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("RP0001")
                .WithNoLocation()
                .WithMessage(""));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task InvalidDependencyRulesFile_ShouldReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", "Invalid JSON content")); // Invalid rules file

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("RP0002")
                .WithNoLocation()
                .WithMessage(""));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

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

    [Fact]
    public async Task ValidDependencyRulesFile_ShouldNotReportDiagnostics_Async()
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
                    """) },
            },
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };
}
