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
        test.TestState.Sources.Add("class TestClass { }");

        test.ExpectedDiagnostics.Add(
            DiagnosticResult.CompilerWarning("RP0001")
                .WithNoLocation()
                .WithMessage(""));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    private AnalyzerTest<DefaultVerifier> GetAnalyzer() =>
        new CSharpAnalyzerTest<ReferenceProtectorAnalyzer, DefaultVerifier>()
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };    
}
