using BenchmarkDotNet.Attributes;

namespace TinyCsv.Benchmarks;

/// <summary>
/// Reads N rows from a string and enumerates the models
/// </summary>
public class ReadBenchmarks : BenchmarkBase
{
    [Benchmark(Baseline = true)]
    public int TinyCsv() => Consume(Libraries.ReadTinyCsv(text));

    [Benchmark]
    public int CsvHelper() => Consume(Libraries.ReadCsvHelper(text));

    [Benchmark]
    public int Sylvan() => Consume(Libraries.ReadSylvan(text));

    [Benchmark]
    public int Sep() => Consume(Libraries.ReadSep(text));

    private static int Consume(IEnumerable<Model> models)
    {
        var checksum = 0;
        foreach (var model in models)
        {
            checksum += model.Id + model.Quantity;
        }
        return checksum;
    }

    /// <summary>
    /// Every library reads the same models
    /// </summary>
    protected override void Verify()
    {
        Check("TinyCsv", Libraries.ReadTinyCsv(text));
        Check("CsvHelper", Libraries.ReadCsvHelper(text));
        Check("Sylvan", Libraries.ReadSylvan(text));
        Check("Sep", Libraries.ReadSep(text));
    }

    private void Check(string library, IEnumerable<Model> read)
    {
        var list = read.ToList();
        if (list.Count != models.Length)
        {
            throw new InvalidOperationException($"{library}: {list.Count} models read, expected {models.Length}");
        }
        for (int i = 0; i < list.Count; i++)
        {
            var (a, b) = (list[i], models[i]);
            if (a.Id != b.Id || a.Name != b.Name || a.Email != b.Email || a.Price != b.Price
                || a.Quantity != b.Quantity || a.CreatedOn != b.CreatedOn || a.Active != b.Active || a.Notes != b.Notes)
            {
                throw new InvalidOperationException($"{library}: the model {i} is different");
            }
        }
    }
}
