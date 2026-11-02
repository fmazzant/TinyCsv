; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
TCSV001 | TinyCsv.Mapping | Error | The column must be a property of the model
TCSV002 | TinyCsv.Mapping | Warning | The property of the column must have a public getter and setter
TCSV003 | TinyCsv.Mapping | Warning | The type of the column needs a converter
TCSV004 | TinyCsv.Mapping | Error | The type must implement the interface and have a public parameterless constructor
TCSV005 | TinyCsv.Mapping | Warning | Two columns have the same index
TCSV006 | TinyCsv.Mapping | Error | MapColumnsByHeader requires HasHeaderRecord
