using System.Text;
using BenchmarkDotNet.Attributes;

namespace TinyCsv.Benchmarks;

/// <summary>
/// Writes N models to a MemoryStream (UTF-8)
/// </summary>
public class WriteBenchmarks : BenchmarkBase
{
    private readonly MemoryStream stream = new();

    [Benchmark(Baseline = true)]
    public long TinyCsv() => Write(Libraries.WriteTinyCsv);

    [Benchmark]
    public long CsvHelper() => Write(Libraries.WriteCsvHelper);

    [Benchmark]
    public long Sylvan() => Write(Libraries.WriteSylvan);

    [Benchmark]
    public long Sep() => Write(Libraries.WriteSep);

    private long Write(Action<Stream, Model[]> write)
    {
        stream.Position = 0;
        stream.SetLength(0);
        write(stream, models);
        return stream.Length;
    }

    /// <summary>
    /// Every library writes the same text, apart from the new line
    /// </summary>
    protected override void Verify()
    {
        var expected = Normalize(text);
        var libraries = new (string Name, Action<Stream, Model[]> Write)[]
        {
            ("TinyCsv", Libraries.WriteTinyCsv),
            ("CsvHelper", Libraries.WriteCsvHelper),
            ("Sylvan", Libraries.WriteSylvan),
            ("Sep", Libraries.WriteSep),
        };
        foreach (var library in libraries)
        {
            Write(library.Write);
            var actual = Normalize(Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length));
            if (actual != expected)
            {
                var index = 0;
                while (index < actual.Length && index < expected.Length && actual[index] == expected[index])
                {
                    index++;
                }
                throw new InvalidOperationException(
                    $"{library.Name}: the text is different at {index}: [{Snippet(actual, index)}] instead of [{Snippet(expected, index)}]");
            }
        }
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n").TrimEnd('\n');

    private static string Snippet(string value, int index) => value.Substring(index, Math.Min(40, value.Length - index));
}
