# TinyCsv Benchmark

[TinyCsv.Benchmarks](TinyCsv.Benchmarks) compares, with [BenchmarkDotNet](https://benchmarkdotnet.org/), the current source of TinyCsv with other CSV libraries for .NET.

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
dotnet run -c Release -f net10.0 -- --filter *                                 # the main libraries, on .NET 10
dotnet run -c Release -f net10.0 -- --filter *Read*                            # only the read
dotnet run -c Release -f net10.0 -- --filter * --job short                     # quicker, less accurate
dotnet run -c Release -f net10.0 -- --filter * --runtimes net8.0 net10.0       # .NET 8 and .NET 10 in the same table
dotnet run -c Release -f net10.0 -- --filter * --anyCategories Main Other      # all the libraries
```

The project targets .NET 8 and .NET 10, so `dotnet run` needs the framework (`-f`).
Running all the libraries is slow, so by default only the `Main` category runs:
TinyCsv, CsvHelper (the most popular library) and Sylvan, Sep and RecordParser (the fastest ones).
The other libraries are in the `Other` category, and their results below are from the last full run.

### Results

| Read 100,000 rows | .NET 8 | .NET 10 | Allocated |
|---|---:|---:|---:|
| Sylvan | 28.4 ms | 24.1 ms | 23.8 MB |
| **TinyCsv** | **31.9 ms** | **25.5 ms** | **23.7 MB** |
| Sep | 46.2 ms | 40.6 ms | 23.7 MB |
| RecordParser | 68.6 ms | 61.4 ms | 23.7 MB |
| NReco.Csv ¹ | 86.5 ms | 83.1 ms | 41.8 MB |
| Csv ¹ | 90.5 ms | 84.2 ms | 73.0 MB |
| TinyCsvParser ¹ | 111.1 ms | 92.4 ms | 68.6 MB |
| CsvHelper | 109.9 ms | 99.4 ms | 53.2 MB |
| LumenWorks ¹ | 126.5 ms | 119.3 ms | 164.5 MB |
| FileHelpers ¹ | 147.0 ms | 126.7 ms | 92.9 MB |
| TextFieldParser ¹ | 471.8 ms | 425.8 ms | 1,026.5 MB |

| Write 100,000 rows | .NET 8 | .NET 10 | Allocated |
|---|---:|---:|---:|
| **TinyCsv** | **36.2 ms** | **27.6 ms** | **0.01 MB** |
| Sylvan | 56.2 ms | 31.4 ms | 0.05 MB |
| Csv ¹ | 44.3 ms | 34.2 ms | 19.6 MB |
| RecordParser | 49.2 ms | 37.4 ms | 0.02 MB |
| NReco.Csv ¹ | 48.1 ms | 41.8 ms | 21.3 MB |
| Sep | 65.2 ms | 42.1 ms | 0.02 MB |
| FileHelpers ¹ | 64.9 ms | 59.9 ms | 103.0 MB |
| CsvHelper | 88.5 ms | 72.8 ms | 25.2 MB |

The times are the means, the libraries are sorted by the .NET 10 time and the allocated memory is the one of .NET 10.
Environment: BenchmarkDotNet 0.15.2, .NET 8.0.29 and .NET 10.0.11, Windows Server 2019 (Hyper-V), Intel Xeon Platinum 8370C 2.80GHz.
The virtual machine is noisy: the absolute times change between runs, so compare the libraries within the same table.
The main libraries are from the last runs (read: about 14 minutes; write: about 18 minutes, after the write allocations of TinyCsv were reduced, [#102](https://github.com/fmazzant/TinyCsv/issues/102); .NET 8 and .NET 10):
the full reports, with 1,000 rows too, are the [read report](results/CompetitiveReadBenchmark-main-report-github.md) and the [write report](results/CompetitiveWriteBenchmark-main-report-github.md).

¹ From the previous run with all the libraries (the [read report](results/CompetitiveReadBenchmark-report-github.md) and the [write report](results/CompetitiveWriteBenchmark-report-github.md)),
not measured again: compare them with the other libraries with care.

The read of TinyCsv creates the string of a record only when it is needed: when the events are enabled, when `SkipRow` is set and for the exceptions.
On .NET 8 and later, with a single char delimiter, the delimiters are found while looking for the end of the record (32 chars at a time, with the vector instructions),
so the fields of a record without quotes are not scanned again.
The `int`, `long` and `decimal` columns read the common values (i.e. `123`, `-1.50`) with a fast parser resolved once for each read, that returns the same result of `TryParse`.
A `DateTime`, `DateTimeOffset` or `DateOnly` column with a fixed width format (i.e. `yyyy-MM-dd` or `dd/MM/yyyy HH:mm:ss`) is read by a fast parser,
that returns the same result of `TryParseExact`: the other formats and the columns without a format use `TryParseExact` and `TryParse`.

The write of TinyCsv builds each line in a reusable buffer: the numbers and the dates are formatted in the buffer (`TryFormat`, on .NET 8 and later)
and the values that need the quotes are escaped in the buffer, so writing does not create a string for each value or line
(the allocated memory is the `StreamWriter` and the setup of the write, the same for 1,000 and 100,000 rows).

## Versions benchmark (retired)

Up to 3.5 the `CsvSampleConsoleBenchmarkApp` project compared the published versions of `Mafe.TinyCsv`: each version was a
BenchmarkDotNet job that referenced the NuGet package, reading (`LoadFromText`) and writing (`SaveToText`) `N` rows of 7 columns.
It has been removed in 4.0.0: the `Mafe.TinyCsv` packages have the old namespaces, so the same code cannot be measured
against them and `Mafesoft.TinyCsv`.

The results of 2.1.0, 2.2.2 and 3.0 are in the [Performance](../README.md#performance) section of the main page;
the full report is in [results](results/TinyCsvLibraryBenchmark-report-github.md).
The 2.2.2 `LoadFromText` result with 10,000 rows is noisy (the median is 11.84 ms).
