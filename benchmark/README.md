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
The results are written to `BenchmarkDotNet.Artifacts/results` (Markdown, CSV and HTML), which is ignored by Git.
To publish new results, copy the `*-report-github.md` file to [results](results/TinyCsvLibraryBenchmark-report-github.md), then add a `#` heading on the first line and the `text` language to the first code block, as required by the linter.

To compare other versions, change the jobs in `TinyCsvLibraryBenchmark.Config`:

```c#
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "2.1.0"));
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "2.2.2"));
AddJob(BaseJob.WithNuGet("Mafe.TinyCsv", "3.0.0"));
```

Each version must be published on NuGet: a job with a version that is not published fails to build and its results are `NA`.

## Results

The results of 2.1.0, 2.2.2 and 3.0.0 are in the [Performance](../README.md#performance) section of the main page.
The 2.2.2 `LoadFromText` result with 10,000 rows is noisy (the median is 11.84 ms).

The full report is in [results](results/TinyCsvLibraryBenchmark-report-github.md).
