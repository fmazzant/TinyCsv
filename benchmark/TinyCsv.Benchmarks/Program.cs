using BenchmarkDotNet.Running;

// By default only the "Main" category runs: TinyCsv, CsvHelper (the most popular), Sylvan, Sep and RecordParser (the fastest).
// Run the main ones:  dotnet run -c Release -f net10.0 -- --filter *
// Run a subset:       dotnet run -c Release -f net10.0 -- --filter *Read*
// Quick (less exact): dotnet run -c Release -f net10.0 -- --filter * --job short
// .NET 8 and 10:      dotnet run -c Release -f net10.0 -- --filter * --runtimes net8.0 net10.0
// All the libraries:  dotnet run -c Release -f net10.0 -- --filter * --anyCategories Main Other
if (!args.Any(a => a is "--anyCategories" or "--allCategories"))
{
    args = args.Concat(new[] { "--anyCategories", "Main" }).ToArray();
}
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
