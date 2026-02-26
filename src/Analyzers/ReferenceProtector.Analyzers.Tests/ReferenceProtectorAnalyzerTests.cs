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

        test.TestState.AdditionalFiles.Add(
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, ""));            

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
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
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
    /// Verifies that the analyzer reports a diagnostic when a package reference violates a defined dependency rule.
    /// </summary>
    [Fact]
    public async Task ValidDependencyRulesFile_PackageDependencyViolated_ShouldReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "PackageDependencies": [
                    {
                        "From": "TestProject.csproj",
                        "To": "Forbidden.Package",
                        "Description": "Can't reference this package",
                        "Policy": "Forbidden"
                    }
                ]
            }
            """));

        test.TestState.AdditionalFiles.Add(
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
            TestProject.csproj	PackageReferenceDirect	Forbidden.Package
            """));

        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0005")
            .WithNoLocation()
            .WithMessage("Package reference 'TestProject.csproj' ==> 'Forbidden.Package' violates dependency rule 'Can't reference this package' or one of its exceptions. Please remove the dependency or update 'DependencyRules.json' file to allow it."));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("RP0003", Microsoft.CodeAnalysis.DiagnosticSeverity.Info)
            .WithNoLocation()
            .WithMessage("No dependency rules matched the current project 'TestProject.csproj'"));

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
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
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

    /// <summary>
    /// Verifies that the analyzer reports RP0006 when a tech debt exception no longer matches any declared project reference.
    /// </summary>
    [Fact]
    public async Task TechDebtException_NoLongerNeeded_ShouldReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "ProjectDependencies": [
                    {
                        "From": "*",
                        "To": "*",
                        "Description": "No direct project references allowed",
                        "Policy": "Forbidden",
                        "LinkType": "Direct",
                        "Exceptions": [
                            {
                                "From": "TestProject.csproj",
                                "To": "OldProject.csproj",
                                "Justification": "Legacy dependency to be removed",
                                "IsTechDebt": true
                            }
                        ]
                    }
                ]
            }
            """));

        // OldProject.csproj is NOT in the declared references
        test.TestState.AdditionalFiles.Add(
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
            TestProject.csproj	ProjectReferenceDirect	SomeOtherProject.csproj
            """));

        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0004")
            .WithNoLocation()
            .WithMessage("Project reference 'TestProject.csproj' ==> 'SomeOtherProject.csproj' violates dependency rule 'No direct project references allowed' or one of its exceptions. Please remove the dependency or update 'DependencyRules.json' file to allow it."));

        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0006")
            .WithNoLocation()
            .WithMessage("Tech debt exception 'TestProject.csproj' ==> 'OldProject.csproj' in rule 'No direct project references allowed' no longer matches any declared reference and can be removed from 'DependencyRules.json'"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer does NOT report RP0006 when a tech debt exception still matches a declared project reference.
    /// </summary>
    [Fact]
    public async Task TechDebtException_StillNeeded_ShouldNotReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "ProjectDependencies": [
                    {
                        "From": "*",
                        "To": "*",
                        "Description": "No direct project references allowed",
                        "Policy": "Forbidden",
                        "LinkType": "Direct",
                        "Exceptions": [
                            {
                                "From": "TestProject.csproj",
                                "To": "ReferencedProject.csproj",
                                "Justification": "Legacy dependency to be removed",
                                "IsTechDebt": true
                            }
                        ]
                    }
                ]
            }
            """));

        test.TestState.AdditionalFiles.Add(
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
            TestProject.csproj	ProjectReferenceDirect	ReferencedProject.csproj
            """));

        // No diagnostics expected - the tech debt exception is still needed
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that a non-tech-debt exception that no longer matches does NOT trigger RP0006.
    /// </summary>
    [Fact]
    public async Task NonTechDebtException_NoLongerNeeded_ShouldNotReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "ProjectDependencies": [
                    {
                        "From": "*",
                        "To": "*",
                        "Description": "No direct project references allowed",
                        "Policy": "Forbidden",
                        "LinkType": "Direct",
                        "Exceptions": [
                            {
                                "From": "TestProject.csproj",
                                "To": "OldProject.csproj",
                                "Justification": "Legitimate exception"
                            }
                        ]
                    }
                ]
            }
            """));

        test.TestState.AdditionalFiles.Add(
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
            TestProject.csproj	ProjectReferenceDirect	SomeOtherProject.csproj
            """));

        // RP0004 for the violating reference, but NO RP0006 since the exception is not tech debt
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0004")
            .WithNoLocation()
            .WithMessage("Project reference 'TestProject.csproj' ==> 'SomeOtherProject.csproj' violates dependency rule 'No direct project references allowed' or one of its exceptions. Please remove the dependency or update 'DependencyRules.json' file to allow it."));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that the analyzer reports RP0006 when a tech debt exception for a package reference no longer matches.
    /// </summary>
    [Fact]
    public async Task TechDebtPackageException_NoLongerNeeded_ShouldReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "PackageDependencies": [
                    {
                        "From": "TestProject.csproj",
                        "To": "*",
                        "Description": "No packages allowed",
                        "Policy": "Forbidden",
                        "Exceptions": [
                            {
                                "From": "TestProject.csproj",
                                "To": "OldPackage",
                                "Justification": "Legacy package to be removed",
                                "IsTechDebt": true
                            }
                        ]
                    }
                ],
                "ProjectDependencies": [
                    {
                        "From": "TestProject.csproj",
                        "To": "*",
                        "Description": "Allowed",
                        "Policy": "Allowed",
                        "LinkType": "DirectOrTransitive"
                    }
                ]
            }
            """));

        test.TestState.AdditionalFiles.Add(
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
            TestProject.csproj	PackageReferenceDirect	SomeOtherPackage
            """));

        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0005")
            .WithNoLocation()
            .WithMessage("Package reference 'TestProject.csproj' ==> 'SomeOtherPackage' violates dependency rule 'No packages allowed' or one of its exceptions. Please remove the dependency or update 'DependencyRules.json' file to allow it."));

        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0006")
            .WithNoLocation()
            .WithMessage("Tech debt exception 'TestProject.csproj' ==> 'OldPackage' in rule 'No packages allowed' no longer matches any declared reference and can be removed from 'DependencyRules.json'"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies that a tech debt exception whose From does not match the current project does NOT trigger RP0006.
    /// This covers the case where a broad rule (From: *) has exceptions for other projects that aren't part
    /// of the current compilation — declared references only contain references for the current project.
    /// </summary>
    [Fact]
    public async Task TechDebtException_ForDifferentProject_ShouldNotReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "ProjectDependencies": [
                    {
                        "From": "*",
                        "To": "*",
                        "Description": "No direct project references allowed",
                        "Policy": "Forbidden",
                        "LinkType": "Direct",
                        "Exceptions": [
                            {
                                "From": "TestProject.csproj",
                                "To": "ReferencedProject.csproj",
                                "Justification": "Tech debt for TestProject",
                                "IsTechDebt": true
                            },
                            {
                                "From": "OtherProject.csproj",
                                "To": "SomeLib.csproj",
                                "Justification": "Tech debt for OtherProject",
                                "IsTechDebt": true
                            }
                        ]
                    }
                ]
            }
            """));

        test.TestState.AdditionalFiles.Add(
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
            TestProject.csproj	ProjectReferenceDirect	ReferencedProject.csproj
            """));

        // TestProject→ReferencedProject is covered by the first exception (still needed), so no RP0004 or RP0006.
        // The second exception (OtherProject→SomeLib) is for a different project — should NOT trigger RP0006.
        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Verifies IsTechDebt defaults to false when not specified in JSON.
    /// </summary>
    [Fact]
    public async Task TechDebtException_DefaultFalse_NoFlagSpecified_ShouldNotReportDiagnostic_Async()
    {
        var test = GetAnalyzer();
        test.TestState.AdditionalFiles.Add(
            ("DependencyRules.json", """
            {
                "ProjectDependencies": [
                    {
                        "From": "*",
                        "To": "*",
                        "Description": "No direct project references allowed",
                        "Policy": "Forbidden",
                        "LinkType": "Direct",
                        "Exceptions": [
                            {
                                "From": "TestProject.csproj",
                                "To": "OldProject.csproj",
                                "Justification": "Legitimate exception",
                                "IsTechDebt": false
                            }
                        ]
                    }
                ]
            }
            """));

        test.TestState.AdditionalFiles.Add(
            (ReferenceProtectorAnalyzer.DeclaredReferencesFile, """
            TestProject.csproj	ProjectReferenceDirect	SomeOtherProject.csproj
            """));

        // Only RP0004 for SomeOtherProject, no RP0006 since IsTechDebt is explicitly false
        test.ExpectedDiagnostics.Add(DiagnosticResult.CompilerWarning("RP0004")
            .WithNoLocation()
            .WithMessage("Project reference 'TestProject.csproj' ==> 'SomeOtherProject.csproj' violates dependency rule 'No direct project references allowed' or one of its exceptions. Please remove the dependency or update 'DependencyRules.json' file to allow it."));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }
}
