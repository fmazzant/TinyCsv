```

BenchmarkDotNet v0.13.7, Windows 10 (10.0.17763.4720/1809/October2018Update/Redstone5) (Hyper-V)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-YGPWRK : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-AXRAKP : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-SPTLKB : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-HMRTYE : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2


```
|       Method |        Job |          NuGetReferences |       N |         Mean |      Error |     StdDev |
|------------- |----------- |------------------------- |-------- |-------------:|-----------:|-----------:|
| **LoadFromText** | **Job-YGPWRK** |       **Mafe.TinyCsv 1.5.2** |    **1000** |     **8.516 ms** |  **0.0349 ms** |  **0.0326 ms** |
|   SaveToText | Job-YGPWRK |       Mafe.TinyCsv 1.5.2 |    1000 |     2.266 ms |  0.0232 ms |  0.0217 ms |
| LoadFromText | Job-AXRAKP |       Mafe.TinyCsv 1.6.1 |    1000 |     8.456 ms |  0.0653 ms |  0.0579 ms |
|   SaveToText | Job-AXRAKP |       Mafe.TinyCsv 1.6.1 |    1000 |     2.287 ms |  0.0221 ms |  0.0207 ms |
| LoadFromText | Job-SPTLKB |       Mafe.TinyCsv 2.0.0 |    1000 |     2.493 ms |  0.0074 ms |  0.0069 ms |
|   SaveToText | Job-SPTLKB |       Mafe.TinyCsv 2.0.0 |    1000 |     1.669 ms |  0.0253 ms |  0.0237 ms |
| LoadFromText | Job-HMRTYE | Mafe.TinyCsv 2.1.0-beta1 |    1000 |     1.128 ms |  0.0030 ms |  0.0028 ms |
|   SaveToText | Job-HMRTYE | Mafe.TinyCsv 2.1.0-beta1 |    1000 |     1.559 ms |  0.0209 ms |  0.0195 ms |
| **LoadFromText** | **Job-YGPWRK** |       **Mafe.TinyCsv 1.5.2** |   **10000** |    **86.102 ms** |  **0.3674 ms** |  **0.3437 ms** |
|   SaveToText | Job-YGPWRK |       Mafe.TinyCsv 1.5.2 |   10000 |    21.543 ms |  0.1216 ms |  0.1138 ms |
| LoadFromText | Job-AXRAKP |       Mafe.TinyCsv 1.6.1 |   10000 |    86.187 ms |  0.4112 ms |  0.3846 ms |
|   SaveToText | Job-AXRAKP |       Mafe.TinyCsv 1.6.1 |   10000 |    22.129 ms |  0.1033 ms |  0.0966 ms |
| LoadFromText | Job-SPTLKB |       Mafe.TinyCsv 2.0.0 |   10000 |    25.256 ms |  0.0753 ms |  0.0704 ms |
|   SaveToText | Job-SPTLKB |       Mafe.TinyCsv 2.0.0 |   10000 |    15.924 ms |  0.1219 ms |  0.1140 ms |
| LoadFromText | Job-HMRTYE | Mafe.TinyCsv 2.1.0-beta1 |   10000 |    11.792 ms |  0.1035 ms |  0.0864 ms |
|   SaveToText | Job-HMRTYE | Mafe.TinyCsv 2.1.0-beta1 |   10000 |    15.176 ms |  0.0624 ms |  0.0583 ms |
| **LoadFromText** | **Job-YGPWRK** |       **Mafe.TinyCsv 1.5.2** |  **100000** |   **858.665 ms** |  **3.0873 ms** |  **2.7368 ms** |
|   SaveToText | Job-YGPWRK |       Mafe.TinyCsv 1.5.2 |  100000 |   218.765 ms |  0.3666 ms |  0.3062 ms |
| LoadFromText | Job-AXRAKP |       Mafe.TinyCsv 1.6.1 |  100000 |   877.641 ms |  3.2216 ms |  2.8559 ms |
|   SaveToText | Job-AXRAKP |       Mafe.TinyCsv 1.6.1 |  100000 |   211.176 ms |  1.2563 ms |  1.1751 ms |
| LoadFromText | Job-SPTLKB |       Mafe.TinyCsv 2.0.0 |  100000 |   251.932 ms |  0.8135 ms |  0.7610 ms |
|   SaveToText | Job-SPTLKB |       Mafe.TinyCsv 2.0.0 |  100000 |   152.877 ms |  0.7557 ms |  0.6699 ms |
| LoadFromText | Job-HMRTYE | Mafe.TinyCsv 2.1.0-beta1 |  100000 |   117.103 ms |  1.0552 ms |  0.9870 ms |
|   SaveToText | Job-HMRTYE | Mafe.TinyCsv 2.1.0-beta1 |  100000 |   142.248 ms |  0.4423 ms |  0.4138 ms |
| **LoadFromText** | **Job-YGPWRK** |       **Mafe.TinyCsv 1.5.2** | **1000000** | **8,444.244 ms** | **52.6833 ms** | **43.9930 ms** |
|   SaveToText | Job-YGPWRK |       Mafe.TinyCsv 1.5.2 | 1000000 | 2,211.665 ms |  7.8473 ms |  7.3403 ms |
| LoadFromText | Job-AXRAKP |       Mafe.TinyCsv 1.6.1 | 1000000 | 8,750.543 ms | 28.8640 ms | 26.9994 ms |
|   SaveToText | Job-AXRAKP |       Mafe.TinyCsv 1.6.1 | 1000000 | 2,149.077 ms |  2.8854 ms |  2.5578 ms |
| LoadFromText | Job-SPTLKB |       Mafe.TinyCsv 2.0.0 | 1000000 | 2,530.445 ms |  8.0288 ms |  7.5101 ms |
|   SaveToText | Job-SPTLKB |       Mafe.TinyCsv 2.0.0 | 1000000 | 1,488.229 ms |  2.9475 ms |  2.6129 ms |
| LoadFromText | Job-HMRTYE | Mafe.TinyCsv 2.1.0-beta1 | 1000000 | 1,157.258 ms |  4.7153 ms |  4.1800 ms |
|   SaveToText | Job-HMRTYE | Mafe.TinyCsv 2.1.0-beta1 | 1000000 | 1,418.138 ms |  3.3791 ms |  3.1608 ms |
