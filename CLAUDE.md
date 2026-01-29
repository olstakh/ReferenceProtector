# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ReferenceProtector is a NuGet package that protects codebases from unwanted dependencies by validating project and package references during build time against user-defined rules. It consists of:

1. **MSBuild Task** (`ReferenceProtector.Tasks`) - Collects project and package references at build time
2. **Roslyn Analyzer** (`ReferenceProtector.Analyzers`) - Validates references against dependency rules and reports diagnostics
3. **Package Project** (`ReferenceProtector.Package`) - Bundles everything into a distributable NuGet package

## Build Commands

```bash
# Build the entire solution
dotnet build ReferenceProtector.sln

# Build a specific project
dotnet build src/Package/ReferenceProtector.Package.csproj

# Build and create NuGet package (outputs to artifacts/ folder)
dotnet build src/Package/ReferenceProtector.Package.csproj -c Release

# Clean build artifacts
dotnet clean
```

## Testing

```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test src/Analyzers/ReferenceProtector.Analyzers.Tests/ReferenceProtector.Analyzers.Tests.csproj
dotnet test src/Tasks/ReferenceProtector.Tasks.UnitTests/ReferenceProtector.Tasks.UnitTests.csproj
dotnet test src/Tasks/ReferenceProtector.Tasks.IntegrationTests/ReferenceProtector.Tasks.IntegrationTests.csproj
dotnet test tests/ReferenceProtector.IntegrationTests/ReferenceProtector.IntegrationTests.csproj

# Run a single test by filter
dotnet test --filter "FullyQualifiedName~TestMethodName"
```

## Architecture

### Two-Phase Build Integration

The package uses a two-phase approach integrated into the MSBuild pipeline:

**Phase 1: MSBuild Task (CollectAllReferences)**
- Runs during `CoreCompileDependsOn` target before compilation
- Declared in `src/Build/ReferenceProtector.targets`
- Task located at `src/Tasks/ReferenceProtector.Tasks/CollectAllReferences.cs`
- Collects all direct and transitive project references from MSBuild's `@(ProjectReference)` items
- Collects direct package references from `@(PackageReference)` items
- Uses `NuGetPackageId` metadata to distinguish transitive project references
- Outputs to `obj/Debug/_ReferenceProtector_DeclaredReferences.tsv` file
- This file is then registered as an `AdditionalFile` for the analyzer

**Phase 2: Roslyn Analyzer (ReferenceProtectorAnalyzer)**
- Located at `src/Analyzers/ReferenceProtector.Analyzers/ReferenceProtector.Analyzers.cs`
- Reads the `_ReferenceProtector_DeclaredReferences.tsv` file via `AdditionalFiles`
- Loads dependency rules from the JSON file specified by `DependencyRulesFile` MSBuild property
- Matches each reference against rules using regex patterns (with `*` expansion)
- Reports diagnostics (RP0001-RP0005) for rule violations

### Key Components

**Shared Models** (`src/Shared/`)
- `ReferenceItem.cs` - Represents a reference (source, target, link type)
- Shared between MSBuild task and analyzer via linked files in project files

**Dependency Rules Schema** (`src/Analyzers/ReferenceProtector.Analyzers/Models/DependencyRules.cs`)
- `ProjectDependencies[]` - Rules for project-to-project references with LinkType support
- `PackageDependencies[]` - Rules for package references (only direct)
- Each rule has: From (regex), To (regex), Policy (Allowed/Forbidden), Description, Exceptions[]

**Configuration**
- `EnableReferenceProtector` MSBuild property controls whether the feature runs (default: true)
- `DependencyRulesFile` MSBuild property specifies the path to the rules JSON file
- These properties are made visible to the analyzer via `CompilerVisibleProperty` in `src/Build/ReferenceProtector.props`

## Project Structure

- `src/Analyzers/` - Roslyn analyzer that validates references
- `src/Tasks/` - MSBuild task that collects references
- `src/Build/` - MSBuild .props and .targets files
- `src/BuildMultiTargeting/` - Multi-targeting MSBuild support
- `src/Package/` - NuGet package project (uses Microsoft.Build.NoTargets SDK)
- `src/Shared/` - Code shared between analyzer and tasks (linked as source)
- `tests/` - Integration tests
- `samples/` - Sample projects demonstrating usage (ClassA, ClassB, ClassC with DependencyRules.json)

## Diagnostic IDs

- **RP0001**: DependencyRulesFile property not provided
- **RP0002**: Invalid dependency rules JSON format
- **RP0003**: No rules matched the current project
- **RP0004**: Project reference violates a rule
- **RP0005**: Package reference violates a rule

## Key Patterns

**Pattern Matching Logic** (`ReferenceProtectorAnalyzer:255`)
- Patterns are regex with `*` replaced by `.*` and `$` appended
- Case-insensitive matching

**Rule Evaluation Logic** (`ReferenceProtectorAnalyzer:192-197`)
- For `Forbidden` policies: violation if no exceptions match
- For `Allowed` policies: violation if any exception matches

**LinkType Handling** (`ReferenceProtectorAnalyzer:180-181`)
- Direct: Matches only `ProjectReferenceDirect`
- Transitive: Matches only `ProjectReferenceTransitive`
- DirectOrTransitive: Matches both

## Dependencies

Central Package Management is enabled (`Directory.Packages.props`). Key dependencies:
- Microsoft.Build.Utilities.Core - for MSBuild task
- Microsoft.CodeAnalysis.CSharp - for Roslyn analyzer
- NuGet.ProjectModel - for NuGet assets parsing
- System.Text.Json - for parsing dependency rules (copied to analyzer output)
- xunit.v3 - for testing

## Versioning

Uses Nerdbank.GitVersioning (`version.json` in repo root) for semantic versioning.

## Development Notes

- The analyzer copies System.Text.Json.dll to its output via a custom target (`CopyDependenciesToOutput` in the .csproj)
- Shared code uses linked compilation (`<Compile Include="..\..\Shared\*.cs" />`)
- Package outputs to `artifacts/` folder
- The sample projects in `samples/` demonstrate the feature with a working `DependencyRules.json`
