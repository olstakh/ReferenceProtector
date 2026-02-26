; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RP0001 | Usage | Warning | Provide DependencyRulesFile property to specify valid dependency rules file.
RP0002 | Usage | Warning | Make sure the dependency rules file '{0}' is in the correct json format
RP0003 | Usage | Info | No dependency rules matched the current project '{0}'
RP0004 | Usage | Warning | Project reference '{0}' ==> '{1}' violates dependency rule '{2}' or one of its exceptions
RP0005 | Usage | Warning | Package reference '{0}' ==> '{1}' violates dependency rule '{2}' or one of its exceptions

## Release 1.3.7

### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RP0006 | Usage | Warning | Technical debt dependency can be removed from the rules exception