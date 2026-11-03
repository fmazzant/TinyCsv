using BenchmarkDotNet.Attributes;

namespace TinyCsv.Benchmarks;

/// <summary>
/// Reads N rows from a string and enumerates the models
/// </summary>
public class ReadBenchmarks : BenchmarkBase
{
    private static readonly (string Name, Func<string, IEnumerable<Model>> Read)[] Libraries =
    [
        ("TinyCsv", Benchmarks.Libraries.ReadTinyCsv),
        ("CsvHelper", Benchmarks.Libraries.ReadCsvHelper),
        ("Sylvan", Benchmarks.Libraries.ReadSylvan),
        ("Sep", Benchmarks.Libraries.ReadSep),
        ("FileHelpers", Benchmarks.Libraries.ReadFileHelpers),
        ("TinyCsvParser", Benchmarks.Libraries.ReadTinyCsvParser),
        ("RecordParser", Benchmarks.Libraries.ReadRecordParser),
        ("Csv", Benchmarks.Libraries.ReadCsv),
        ("NReco.Csv", Benchmarks.Libraries.ReadNReco),
        ("LumenWorks", Benchmarks.Libraries.ReadLumenWorks),
        ("TextFieldParser", Benchmarks.Libraries.ReadTextFieldParser),
    ];

    [Benchmark(Baseline = true)]
    public int TinyCsv() => Consume(Benchmarks.Libraries.ReadTinyCsv(text));

    [Benchmark]
    public int CsvHelper() => Consume(Benchmarks.Libraries.ReadCsvHelper(text));

    [Benchmark]
    public int Sylvan() => Consume(Benchmarks.Libraries.ReadSylvan(text));

    [Benchmark]
    public int Sep() => Consume(Benchmarks.Libraries.ReadSep(text));

    [Benchmark]
    public int FileHelpers() => Consume(Benchmarks.Libraries.ReadFileHelpers(text));

    [Benchmark]
    public int TinyCsvParser() => Consume(Benchmarks.Libraries.ReadTinyCsvParser(text));

    [Benchmark]
    public int RecordParser() => Consume(Benchmarks.Libraries.ReadRecordParser(text));

    [Benchmark]
    public int Csv() => Consume(Benchmarks.Libraries.ReadCsv(text));

    [Benchmark]
    public int NRecoCsv() => Consume(Benchmarks.Libraries.ReadNReco(text));

    [Benchmark]
    public int LumenWorks() => Consume(Benchmarks.Libraries.ReadLumenWorks(text));

    [Benchmark]
    public int TextFieldParser() => Consume(Benchmarks.Libraries.ReadTextFieldParser(text));

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
        foreach (var library in Libraries)
        {
            Check(library.Name, library.Read(text));
        }
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
