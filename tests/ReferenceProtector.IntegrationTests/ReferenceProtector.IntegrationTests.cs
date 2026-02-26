using Xunit;

namespace ReferenceProtector.IntegrationTests;

/// <summary>
/// This class tests the consumption of the ReferenceProtector package.
/// </summary>
public class ReferenceProtectorIntegrationTests : TestBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceProtectorIntegrationTests"/> class.
    /// </summary>
    public ReferenceProtectorIntegrationTests(ITestOutputHelper output) : base(output)
    {
    }

    /// <summary>
    /// Referencing the ReferenceProtector package without providing a DependencyRulesFile should produce a warning.
    /// </summary>
    [Fact]
    public async Task PackageReference_ProducesWarning_Async()
    {
        CreateProject("A");
        var warnings = await Build();

        var warning = Assert.Single(warnings);
        Assert.Equal(new Warning()
        {
            Message = "RP0001: Provide DependencyRulesFile property to specify valid dependency rules file. Current path: N/A.",
            Project = "A/A.csproj",
        }, warning);
    }

    /// <summary>
    /// Referencing the ReferenceProtector package with a valid DependencyRulesFile should not produce any warnings.
    /// </summary>
    [Fact]
    public async Task PackageReference_DependencyRules_NoWarnings_Async()
    {
        CreateProject("A");
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, """
{
}
""");
        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath}");

        Assert.Empty(warnings);
    }

    /// <summary>
    /// Referencing the ReferenceProtector package with an invalid DependencyRulesFile should produce a warning.
    /// </summary>
    [Fact]
    public async Task PackageReference_DependencyRulesInvalid_ProducesWarnings_Async()
    {
        CreateProject("A");
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, """
bad json
""");
        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath}");

        var warning = Assert.Single(warnings);
        Assert.Equal(new Warning()
        {
            Message = $"RP0002: Make sure the dependency rules file '{testRulesPath}' is in the correct json format",
            Project = "A/A.csproj",
        }, warning);
    }

    /// <summary>
    /// Validates that dependency policy violation will produce a build warning.
    /// </summary>
    [Fact]
    public async Task PackageReference_DependencyRuleViolated_ProducesWarnings_Async()
    {
        var projectA = CreateProject("A");
        var projectB = CreateProject("B");
        await AddProjectReference("A", "B");
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, $$"""
        {
            "ProjectDependencies": [
            {
                "From": "*",
                "To": "{{projectB.Replace("\\", "\\\\")}}",
                "LinkType": "Direct",
                "Policy": "Forbidden",
                "Description": "test rule"
            }           
            ]
        }
    """);

        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath}");

        var warning = Assert.Single(warnings);
        Assert.Equal(new Warning()
        {
            Message = $"RP0004: Project reference '{projectA}' ==> '{projectB}' violates dependency rule 'test rule' or one of its exceptions. Please remove the dependency or update '{testRulesPath}' file to allow it.",
            Project = "A/A.csproj",
        }, warning);
    }

    /// <summary>
    /// Validates that dependency policy violation for package references will produce a build warning.
    /// </summary>
    [Fact]
    public async Task PackageReference_PackageDependencyRuleViolated_ProducesWarnings_Async()
    {
        var projectA = CreateProject("A");
        var packageX = "System.Text.Json";
        await AddPackageReference("A", packageX);
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, $$"""
        {
            "PackageDependencies": [
            {
                "From": "*",
                "To": "System.Text.*",
                "Policy": "Forbidden",
                "Description": "test rule"
            }           
            ]
        }
    """);

        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath}");

        var warning = Assert.Single(warnings);
        Assert.Equal(new Warning()
        {
            Message = $"RP0005: Package reference '{projectA}' ==> '{packageX}' violates dependency rule 'test rule' or one of its exceptions. Please remove the dependency or update '{testRulesPath}' file to allow it.",
            Project = "A/A.csproj",
        }, warning);
    }

    /// <summary>
    /// Validates that dependency policy violation will not produce a build warning when the feature is disabled.
    /// </summary>
    [Fact]
    public async Task PackageReference_DependencyRuleViolated_FeatureDisabled_NoWarnings_Async()
    {
        var projectA = CreateProject("A");
        var projectB = CreateProject("B");
        await AddProjectReference("A", "B");
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, $$"""
        {
            "ProjectDependencies": [
            {
                "From": "*",
                "To": "{{projectB.Replace("\\", "\\\\")}}",
                "LinkType": "Direct",
                "Policy": "Forbidden",
                "Description": "test rule"
            }           
            ]
        }
    """);

        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath} /p:EnableReferenceProtector=false");

        Assert.Empty(warnings);
    }

    /// <summary>
    /// Validates that a stale tech debt exception (one that no longer matches any declared reference) produces an RP0006 warning.
    /// </summary>
    [Fact]
    public async Task TechDebtException_NoLongerNeeded_ProducesWarning_Async()
    {
        var projectA = CreateProject("A");
        var projectB = CreateProject("B");
        await AddProjectReference("A", "B");
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, $$"""
        {
            "ProjectDependencies": [
            {
                "From": "{{projectA.Replace("\\", "\\\\")}}",
                "To": "*",
                "LinkType": "Direct",
                "Policy": "Forbidden",
                "Description": "test rule",
                "Exceptions": [
                    {
                        "From": "{{projectA.Replace("\\", "\\\\")}}",
                        "To": "{{projectB.Replace("\\", "\\\\")}}",
                        "Justification": "Current tech debt",
                        "IsTechDebt": true
                    },
                    {
                        "From": "{{projectA.Replace("\\", "\\\\")}}",
                        "To": "*OldProject.csproj",
                        "Justification": "Stale tech debt",
                        "IsTechDebt": true
                    }
                ]
            }           
            ]
        }
    """);

        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath}");

        // The exception for A→B still matches, so no RP0006 for it.
        // The exception for A→*OldProject.csproj is stale, so RP0006 is expected.
        var warning = Assert.Single(warnings);
        Assert.Equal(new Warning()
        {
            Message = $"RP0006: Tech debt exception '{projectA}' ==> '*OldProject.csproj' in rule 'test rule' no longer matches any declared reference and can be removed from '{testRulesPath}'",
            Project = "A/A.csproj",
        }, warning);
    }

    /// <summary>
    /// Validates that a tech debt exception that still matches a declared reference does NOT produce an RP0006 warning.
    /// </summary>
    [Fact]
    public async Task TechDebtException_StillNeeded_NoWarning_Async()
    {
        var projectA = CreateProject("A");
        var projectB = CreateProject("B");
        await AddProjectReference("A", "B");
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, $$"""
        {
            "ProjectDependencies": [
            {
                "From": "{{projectA.Replace("\\", "\\\\")}}",
                "To": "*",
                "LinkType": "Direct",
                "Policy": "Forbidden",
                "Description": "test rule",
                "Exceptions": [
                    {
                        "From": "{{projectA.Replace("\\", "\\\\")}}",
                        "To": "{{projectB.Replace("\\", "\\\\")}}",
                        "Justification": "Current tech debt - still needed",
                        "IsTechDebt": true
                    }
                ]
            }           
            ]
        }
    """);

        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath}");

        // The tech debt exception still matches A→B, so no RP0006.
        // And the exception suppresses the RP0004 violation.
        Assert.Empty(warnings);
    }

    /// <summary>
    /// Validates that a tech debt exception scoped to a different project does NOT produce RP0006 during the current project's compilation.
    /// </summary>
    [Fact]
    public async Task TechDebtException_ForDifferentProject_NoWarning_Async()
    {
        var projectA = CreateProject("A");
        var projectB = CreateProject("B");
        await AddProjectReference("A", "B");
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, $$"""
        {
            "ProjectDependencies": [
            {
                "From": "*",
                "To": "*",
                "LinkType": "Direct",
                "Policy": "Forbidden",
                "Description": "test rule",
                "Exceptions": [
                    {
                        "From": "{{projectA.Replace("\\", "\\\\")}}",
                        "To": "{{projectB.Replace("\\", "\\\\")}}",
                        "Justification": "Tech debt for A",
                        "IsTechDebt": true
                    },
                    {
                        "From": "{{projectB.Replace("\\", "\\\\")}}",
                        "To": "*SomeOtherProject.csproj",
                        "Justification": "Tech debt for B, not relevant for A",
                        "IsTechDebt": true
                    }
                ]
            }           
            ]
        }
    """);

        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath}");

        // A→B is covered by the first exception (still needed), so no RP0004 or RP0006 for A.
        // The second exception (B→*SomeOtherProject.csproj) is for project B, so project A should NOT report RP0006 for it.
        // Project B has no references, so no warnings from B either.
        Assert.Empty(warnings);
    }

    /// <summary>
    /// Validates that a non-tech-debt exception that no longer matches does NOT produce an RP0006 warning.
    /// </summary>
    [Fact]
    public async Task NonTechDebtException_NoLongerNeeded_NoWarning_Async()
    {
        var projectA = CreateProject("A");
        var projectB = CreateProject("B");
        await AddProjectReference("A", "B");
        var testRulesPath = Path.Combine(TestDirectory, "testRules.json");
        File.WriteAllText(testRulesPath, $$"""
        {
            "ProjectDependencies": [
            {
                "From": "{{projectA.Replace("\\", "\\\\")}}",
                "To": "*",
                "LinkType": "Direct",
                "Policy": "Forbidden",
                "Description": "test rule",
                "Exceptions": [
                    {
                        "From": "{{projectA.Replace("\\", "\\\\")}}",
                        "To": "{{projectB.Replace("\\", "\\\\")}}",
                        "Justification": "Legitimate exception"
                    },
                    {
                        "From": "{{projectA.Replace("\\", "\\\\")}}",
                        "To": "*OldProject.csproj",
                        "Justification": "Legitimate exception, not tech debt"
                    }
                ]
            }           
            ]
        }
    """);

        var warnings = await Build(additionalArgs:
            $"/p:DependencyRulesFile={testRulesPath}");

        // No RP0006 because neither exception has IsTechDebt=true.
        // A→B is covered by the first exception, so no RP0004 either.
        Assert.Empty(warnings);
    }
}