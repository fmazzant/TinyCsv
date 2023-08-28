```

BenchmarkDotNet v0.13.7, Windows 10 (10.0.17763.4720/1809/October2018Update/Redstone5) (Hyper-V)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-ZMQJQY : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2


```
|       Method |        Job |          NuGetReferences |    N |     Mean |     Error |    StdDev |
|------------- |----------- |------------------------- |----- |---------:|----------:|----------:|
| LoadFromText | Job-MVYMIX |       Mafe.TinyCsv 2.0.0 | 1000 |       NA |        NA |        NA |
| LoadFromText | Job-ZMQJQY | Mafe.TinyCsv 2.1.0-beta1 | 1000 | 1.116 ms | 0.0050 ms | 0.0042 ms |

Benchmarks with issues:
  SystemDataBenchmark.LoadFromText: Job-MVYMIX(NuGetReferences=Mafe.TinyCsv 2.0.0) [N=1000]
