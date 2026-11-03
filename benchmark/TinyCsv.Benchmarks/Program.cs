using BenchmarkDotNet.Running;

// Run all:            dotnet run -c Release -f net10.0
// Run a subset:       dotnet run -c Release -f net10.0 -- --filter *Read*
// Quick (less exact): dotnet run -c Release -f net10.0 -- --filter * --job short
// .NET 8 and 10:      dotnet run -c Release -f net10.0 -- --runtimes net8.0 net10.0
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
