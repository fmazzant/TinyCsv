using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace TinyCsv.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
[CategoriesColumn]
public abstract class BenchmarkBase
{
    [Params(1_000, 100_000)]
    public int N;

    protected Model[] models = [];
    protected string text = "";

    [GlobalSetup]
    public void Setup()
    {
        models = Data.CreateModels(N);
        text = Data.CreateText(models);
        Verify();
    }

    /// <summary>
    /// Checks that every benchmark of the class produces the same result
    /// </summary>
    protected abstract void Verify();
}
