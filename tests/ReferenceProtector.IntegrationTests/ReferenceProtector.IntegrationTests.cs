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
            Message = $"RP0004: Project reference '{projectA}' ==> '{projectB}' violates dependency rule 'test rule' or one of its exceptions",
            Project = "A/A.csproj",
        }, warning);
    }
}