# TinyCsv Benchmark

There are two benchmarks, both with [BenchmarkDotNet](https://benchmarkdotnet.org/):

| Project | Compares |
|---|---|
| [TinyCsv.Benchmarks](TinyCsv.Benchmarks) | the current source of TinyCsv with other CSV libraries for .NET |
| [CsvSampleConsoleBenchmarkApp](CsvSampleConsoleBenchmarkApp) | the published versions of TinyCsv |

## Competitive benchmark

`TinyCsv.Benchmarks` references the TinyCsv project, so it measures the code of the repository, and compares it with:

| Library | Version | Read | Write | Mapping |
|---|---|:---:|:---:|---|
| [CsvHelper](https://joshclose.github.io/CsvHelper/) | 33.1.0 | ✓ | ✓ | automatic, by header name |
| [Sylvan.Data.Csv](https://github.com/MarkPflug/Sylvan) | 1.4.3 | ✓ | ✓ | hand written (`GetInt32`, `GetDecimal`, ...), `Sylvan.Data` to write |
| [Sep](https://github.com/nietras/Sep) | 0.11.0 | ✓ | ✓ | hand written (`Parse<int>`, `Format`, ...) |
| [FileHelpers](https://www.filehelpers.net/) | 3.5.2 | ✓ | ✓ | attributes on a record class with fields, copied to and from the model |
| [TinyCsvParser](https://github.com/bytefish/TinyCsvParser) | 3.0.11 | ✓ | | `CsvMapping<T>` by column index |
| [RecordParser](https://github.com/leandromoh/RecordParser) | 2.3.0 | ✓ | ✓ | builders by column index, without parallelism |
| [Csv](https://github.com/stevehansen/csv) | 3.0.0 | ✓ | ✓ | hand written, from and to strings |
| [NReco.Csv](https://github.com/nreco/csv) | 1.0.3 | ✓ | ✓ | hand written, from and to strings |
| [LumenWorksCsvReader](https://github.com/phatcher/CsvReader) | 4.0.0 | ✓ | | hand written, from strings |
| [TextFieldParser](https://learn.microsoft.com/dotnet/api/microsoft.visualbasic.fileio.textfieldparser) | .NET 8 | ✓ | | hand written, from strings |

TinyCsv, CsvHelper, FileHelpers, TinyCsvParser and RecordParser map the columns to the properties (FileHelpers to the fields of its record class).
Sylvan and Sep are used with a hand written mapping, as their documentation suggests:
they are lower level libraries, so they are the reference of the fastest possible result.
Csv, NReco.Csv, LumenWorks and `TextFieldParser` (the parser of `Microsoft.VisualBasic.FileIO`, in the .NET runtime) return the values as strings,
that are converted by the same hand written mapping.
TinyCsvParser, LumenWorks and `TextFieldParser` only read.

In TinyCsvParser 3.0, a mapped `string` property keeps the doubled quotes of a quoted value (i.e. `""delimiter""`),
so `Notes` is mapped with `MapUsing` and `CsvRow.GetString`, that removes them.

### What is measured

The model has 8 columns of different types (`int`, `string`, `decimal`, `DateTime`, `bool`), the delimiter is `;` and the culture is invariant:

```csv
Id;Name;Email;Price;Quantity;CreatedOn;Active;Notes
1;Name 1;user1@mywebsite.it;1.12;0;2022-04-02;False;"note; with ""delimiter"" 0"
2;Name 2;user2@mywebsite.it;1.49;1;2022-04-03;True;note 1
```

One row out of 10 has a quoted value with the delimiter and escaped quotes inside.

| Benchmark | Description |
|---|---|
| `ReadBenchmarks` | reads `N` rows from a string and enumerates the models |
| `WriteBenchmarks` | writes `N` models to a `MemoryStream` (UTF-8), escaping the values as RFC 4180 |

`N` is 1,000 and 100,000 rows.
Before measuring, the setup checks that every library reads the same models and writes the same text.

### Run

```bash
cd benchmark/TinyCsv.Benchmarks
dotnet run -c Release                                   # all, about 10 minutes
dotnet run -c Release -- --filter *Read*                # only the read
dotnet run -c Release -- --filter * --job short         # quicker, less accurate
```

### Results

| Read 100,000 rows | Mean | Median | Allocated |
|---|---:|---:|---:|
| Sylvan | 28.3 ms | 28.4 ms | 23.8 MB |
| Sep | 53.8 ms | 45.1 ms | 23.7 MB |
| **TinyCsv** | **53.7 ms** | **53.4 ms** | **23.7 MB** |
| CsvHelper | 119.1 ms | 115.9 ms | 53.2 MB |

| Write 100,000 rows | Mean | Allocated |
|---|---:|---:|
| **TinyCsv** | **36.6 ms** | **2.2 MB** |
| Sylvan | 38.5 ms | 0.05 MB |
| Sep | 45.9 ms | 0.02 MB |
| CsvHelper | 80.7 ms | 25.2 MB |

Environment: BenchmarkDotNet 0.14.0, .NET 8.0.29, Windows Server 2019 (Hyper-V), Intel Xeon Platinum 8370C 2.80GHz.
The Sep read is noisy on this machine (standard deviation 12.9 ms), so its median is more meaningful than its mean.
The full reports, with 1,000 rows too, are the [read report](results/CompetitiveReadBenchmark-report-github.md) and the [write report](results/CompetitiveWriteBenchmark-report-github.md).

The read of TinyCsv creates the string of a record only when it is needed: when the events are enabled, when `SkipRow` is set and for the exceptions.
A `DateTime`, `DateTimeOffset` or `DateOnly` column with a fixed width format (i.e. `yyyy-MM-dd` or `dd/MM/yyyy HH:mm:ss`) is read by a fast parser,
that returns the same result of `TryParseExact`: the other formats and the columns without a format use `TryParseExact` and `TryParse`.

## Versions benchmark

`CsvSampleConsoleBenchmarkApp` compares the published versions of TinyCsv.
Each version is a job that references the `Mafe.TinyCsv` NuGet package, so the same code is measured against every version.

The model has 7 string columns, and each row is:

```csv
1;Name 1;1.12;02/04/2022;aGVsbG8sIHdvcmxkIQ==;https://www.mywebsite.it;A;
```

| Benchmark | Description |
|---|---|
| `LoadFromText` | reads `N` rows from a string and enumerates the models |
| `SaveToText` | writes `N` models to a `MemoryStream` |

`N` is 100, 1,000, 10,000, 100,000 and 1,000,000 rows.
The `MemoryDiagnoser` reports the allocated memory of each operation.

### Run the versions benchmark

```bash
cd benchmark/CsvSampleConsoleBenchmarkApp
dotnet run -c Release
```

The run takes about 15 minutes with 3 versions.
The results are written to `BenchmarkDotNet.Artifacts/results` (Markdown, CSV and HTML), which is ignored by Git.
To publish new results, copy the `*-report-github.md` file to [results](results/TinyCsvLibraryBenchmark-report-github.md), then add a `#` heading on the first line and the `text` language to the first code block, as required by the linter.

To compare other versions, change the jobs in `TinyCsvLibraryBenchmark.Config`:

```c#
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "2.2.2"));
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "3.2.0"));
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "3.5.0"));
```

Each version must be published on NuGet: a job with a version that is not published fails to build and its results are `NA`.

### Versions results

The results of 2.1.0, 2.2.2 and 3.0 are in the [Performance](../README.md#performance) section of the main page.
The 2.2.2 `LoadFromText` result with 10,000 rows is noisy (the median is 11.84 ms).

The full report is in [results](results/TinyCsvLibraryBenchmark-report-github.md).
