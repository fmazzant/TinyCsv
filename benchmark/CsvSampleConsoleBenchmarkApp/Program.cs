using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using TinyCsv;

var summary = BenchmarkRunner.Run<TinyCsvLibraryBenchmark>();

public class TinyCsvBenchmarkBase
{
    protected static Job BaseJob = Job.Default;

    protected string row = "1;Name 1;1.12;02/04/2022;aGVsbG8sIHdvcmxkIQ==;https://www.mywebsite.it;A;";
    protected string[]? stringArray;
    protected Model[]? modelArray;
    protected string? text = null;

    [Params(1000)]//, 10000, 100000, 1000000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        this.stringArray = new string[N];
        for (int i = 0; i < N; i++)
        {
            stringArray[i] = row;
        }
        this.text = string.Join(Environment.NewLine, stringArray);

        this.modelArray = new Model[N];
        for (int i = 0; i < N; i++)
        {
            modelArray[i] = new Model
            {
                Id = "1",
                Name = "Name 1",
                Price = "1.12",
                CreatedOn = "02/04/2022",
                TextBase64 = "aGVsbG8sIHdvcmxkIQ==",
                WebSite = "https://www.mywebsite.it",
                RowType = "A"
            };
        }
    }

    [Benchmark]
    public void LoadFromText()
    {
        var models = tinyCsv.LoadFromText(text);
        foreach (var m in models)
        {
            ;
        }
    }

    [Benchmark]
    public void SaveToText()
    {
        var memoryStream = new MemoryStream();
        tinyCsv.Save(memoryStream, modelArray);
    }

    public class Model
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public string Price { get; set; }
        public string CreatedOn { get; set; }
        public string TextBase64 { get; set; }
        public string WebSite { get; set; }
        public string RowType { get; set; }
    }

    TinyCsv<Model> tinyCsv = new TinyCsv<Model>(options =>
    {
        // Options
        options.HasHeaderRecord = true;
        options.Delimiter = ";";
        options.RowsToSkip = 0;
        options.SkipRow = (row, idx) => string.IsNullOrWhiteSpace(row) || row.StartsWith("#");
        options.TrimData = true;
        options.ValidateColumnCount = false;

        // Columns
        options.Columns.AddColumn(m => m.Id);
        options.Columns.AddColumn(m => m.Name);
        options.Columns.AddColumn(m => m.Price);
        options.Columns.AddColumn(m => m.CreatedOn);
        options.Columns.AddColumn(m => m.TextBase64);
        options.Columns.AddColumn(m => m.WebSite);
        options.Columns.AddColumn(m => m.RowType);
    });
}

[Config(typeof(Config))]
public class TinyCsvLibraryBenchmark : TinyCsvBenchmarkBase
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "1.5.2"));
            AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "1.6.1"));
            AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "2.0.0"));
            AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "2.1.0-beta1"));
        }
    }
}

