```

BenchmarkDotNet v0.13.7, Windows 10 (10.0.17763.4720/1809/October2018Update/Redstone5) (Hyper-V)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-BEWSRH : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-YLLKQQ : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-RYFZVN : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  Job-PZYJSX : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2


```
|       Method |        Job |          NuGetReferences |       N |           Mean |        Error |       StdDev |
|------------- |----------- |------------------------- |-------- |---------------:|-------------:|-------------:|
| **LoadFromText** | **Job-BEWSRH** |       **Mafe.TinyCsv 1.5.2** |     **100** |       **846.0 μs** |      **5.45 μs** |      **4.55 μs** |
|   SaveToText | Job-BEWSRH |       Mafe.TinyCsv 1.5.2 |     100 |       205.4 μs |      0.56 μs |      0.47 μs |
| LoadFromText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 |     100 |       861.7 μs |      7.04 μs |      6.58 μs |
|   SaveToText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 |     100 |       212.9 μs |      1.11 μs |      0.99 μs |
| LoadFromText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 |     100 |       251.1 μs |      3.17 μs |      2.81 μs |
|   SaveToText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 |     100 |       151.7 μs |      0.70 μs |      0.58 μs |
| LoadFromText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 |     100 |       113.4 μs |      1.25 μs |      1.17 μs |
|   SaveToText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 |     100 |       136.9 μs |      0.38 μs |      0.34 μs |
| **LoadFromText** | **Job-BEWSRH** |       **Mafe.TinyCsv 1.5.2** |    **1000** |     **8,512.3 μs** |     **28.76 μs** |     **25.50 μs** |
|   SaveToText | Job-BEWSRH |       Mafe.TinyCsv 1.5.2 |    1000 |     2,279.2 μs |     13.20 μs |     11.70 μs |
| LoadFromText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 |    1000 |     8,761.9 μs |     54.74 μs |     51.20 μs |
|   SaveToText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 |    1000 |     2,333.6 μs |     30.10 μs |     28.16 μs |
| LoadFromText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 |    1000 |     2,469.0 μs |      6.99 μs |      6.20 μs |
|   SaveToText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 |    1000 |     1,642.3 μs |     24.38 μs |     22.81 μs |
| LoadFromText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 |    1000 |     1,126.3 μs |      4.64 μs |      3.88 μs |
|   SaveToText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 |    1000 |     1,528.4 μs |     30.13 μs |     32.24 μs |
| **LoadFromText** | **Job-BEWSRH** |       **Mafe.TinyCsv 1.5.2** |   **10000** |    **86,277.2 μs** |    **515.32 μs** |    **456.82 μs** |
|   SaveToText | Job-BEWSRH |       Mafe.TinyCsv 1.5.2 |   10000 |    22,194.3 μs |    333.63 μs |    312.08 μs |
| LoadFromText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 |   10000 |    86,790.3 μs |    731.04 μs |    683.82 μs |
|   SaveToText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 |   10000 |    22,144.5 μs |    213.38 μs |    189.15 μs |
| LoadFromText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 |   10000 |    25,619.5 μs |    155.18 μs |    137.56 μs |
|   SaveToText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 |   10000 |    15,996.0 μs |    127.80 μs |    119.54 μs |
| LoadFromText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 |   10000 |    11,939.1 μs |    114.13 μs |    101.18 μs |
|   SaveToText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 |   10000 |    15,278.2 μs |    167.86 μs |    157.01 μs |
| **LoadFromText** | **Job-BEWSRH** |       **Mafe.TinyCsv 1.5.2** |  **100000** |   **858,707.4 μs** |  **4,632.75 μs** |  **4,333.48 μs** |
|   SaveToText | Job-BEWSRH |       Mafe.TinyCsv 1.5.2 |  100000 |   218,451.5 μs |  1,443.76 μs |  1,279.86 μs |
| LoadFromText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 |  100000 |   868,503.4 μs |  4,264.51 μs |  3,561.06 μs |
|   SaveToText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 |  100000 |   212,809.3 μs |    533.54 μs |    472.97 μs |
| LoadFromText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 |  100000 |   252,924.8 μs |  2,966.48 μs |  2,629.70 μs |
|   SaveToText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 |  100000 |   150,988.3 μs |    463.69 μs |    411.05 μs |
| LoadFromText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 |  100000 |   118,511.3 μs |  1,636.50 μs |  1,530.79 μs |
|   SaveToText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 |  100000 |   143,671.9 μs |    949.45 μs |    888.12 μs |
| **LoadFromText** | **Job-BEWSRH** |       **Mafe.TinyCsv 1.5.2** | **1000000** | **9,050,792.4 μs** | **35,990.79 μs** | **33,665.81 μs** |
|   SaveToText | Job-BEWSRH |       Mafe.TinyCsv 1.5.2 | 1000000 | 2,098,261.6 μs | 17,451.93 μs | 16,324.55 μs |
| LoadFromText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 | 1000000 | 8,676,067.1 μs | 15,892.13 μs | 13,270.65 μs |
|   SaveToText | Job-YLLKQQ |       Mafe.TinyCsv 1.6.1 | 1000000 | 2,140,552.9 μs |  5,345.33 μs |  4,738.50 μs |
| LoadFromText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 | 1000000 | 2,544,880.7 μs | 10,738.34 μs |  9,519.26 μs |
|   SaveToText | Job-RYFZVN |       Mafe.TinyCsv 2.0.0 | 1000000 | 1,510,920.8 μs | 19,056.55 μs | 17,825.50 μs |
| LoadFromText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 | 1000000 | 1,194,040.5 μs |  7,529.15 μs |  6,287.18 μs |
|   SaveToText | Job-PZYJSX | Mafe.TinyCsv 2.1.0-beta1 | 1000000 | 1,468,220.3 μs |  9,056.31 μs |  8,471.27 μs |
