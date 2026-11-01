# TinyCsv Benchmark

The benchmark compares the published versions of TinyCsv with [BenchmarkDotNet](https://benchmarkdotnet.org/).
Each version is a job that references the `Mafe.TinyCsv` NuGet package, so the same code is measured against every version.

## What is measured

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

## Run

```bash
cd benchmark/CsvSampleConsoleBenchmarkApp
dotnet run -c Release
```

The run takes about 15 minutes with 3 versions.
The results are written to `BenchmarkDotNet.Artifacts/results` (markdown, csv and html).

To compare other versions, change the jobs in `TinyCsvLibraryBenchmark.Config`:

```c#
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "2.1.0"));
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "2.2.2"));
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "3.0.0"));
```

Each version must be published on NuGet: a job with a version that is not published fails to build and its results are `NA`.

## Results

Each cell is the mean time and the allocated memory.

### LoadFromText

| N | 2.1.0 | 2.2.2 | 3.0.0 |
|---:|---:|---:|---:|
| 100 | 0.072 ms / 119 KB | 0.106 ms / 195 KB | **0.065 ms / 94 KB** |
| 1,000 | 0.727 ms / 1.14 MB | 1.067 ms / 1.89 MB | **0.647 ms / 0.82 MB** |
| 10,000 | 8.39 ms / 11.40 MB | 14.62 ms / 18.88 MB | **7.42 ms / 8.13 MB** |
| 100,000 | 74.4 ms / 114.0 MB | 110.6 ms / 188.7 MB | **71.5 ms / 81.2 MB** |
| 1,000,000 | 772 ms / 1,140 MB | 1,099 ms / 1,887 MB | **679 ms / 812 MB** |

### SaveToText

| N | 2.1.0 | 2.2.2 | 3.0.0 |
|---:|---:|---:|---:|
| 100 | 0.092 ms / 107 KB | 0.093 ms / 107 KB | 0.100 ms / 124 KB |
| 1,000 | 1.117 ms / 1.10 MB | 1.120 ms / 1.10 MB | 1.105 ms / 1.26 MB |
| 10,000 | 11.88 ms / 10.47 MB | 11.47 ms / 10.47 MB | 12.71 ms / 12.08 MB |
| 100,000 | 94.4 ms / 100.7 MB | 95.8 ms / 100.7 MB | 104.4 ms / 116.7 MB |
| 1,000,000 | 988 ms / 1,103 MB | 983 ms / 1,103 MB | 1,107 ms / 1,263 MB |

Environment: BenchmarkDotNet 0.13.12, .NET 8.0.29, Windows Server 2019 (Hyper-V), Intel Xeon Platinum 8370C 2.80GHz.
The 3.0.0 column was measured with the 3.0.0-rc1 package, which has the same code as 3.0.0.
The 2.2.2 `LoadFromText` result with 10,000 rows is noisy (the median is 11.84 ms).

The full report is in [results](results/TinyCsvLibraryBenchmark-report-github.md), and the comments on the results are in the [Performance](../README.md#performance) section of the main README.
