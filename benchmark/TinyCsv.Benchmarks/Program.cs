using BenchmarkDotNet.Running;

// Run all:            dotnet run -c Release
// Run a subset:       dotnet run -c Release -- --filter *Read*
// Quick (less exact): dotnet run -c Release -- --filter * --job short
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
