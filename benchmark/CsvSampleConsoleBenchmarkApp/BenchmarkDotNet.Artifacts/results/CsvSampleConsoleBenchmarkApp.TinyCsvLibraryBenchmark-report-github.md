```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.17763.9376/1809/October2018Update/Redstone5) (Hyper-V)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 2 logical cores and 1 physical core
.NET SDK 10.0.302
  [Host]     : .NET 8.0.29 (8.0.2926.32403), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-DLAJMG : .NET 8.0.29 (8.0.2926.32403), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-BHLBDE : .NET 8.0.29 (8.0.2926.32403), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JWNPKE : .NET 8.0.29 (8.0.2926.32403), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method       | Job        | NuGetReferences        | N       | Mean            | Error         | StdDev        | Median          | Gen0       | Gen1     | Gen2     | Allocated     |
|------------- |----------- |----------------------- |-------- |----------------:|--------------:|--------------:|----------------:|-----------:|---------:|---------:|--------------:|
| **LoadFromText** | **Job-DLAJMG** | **Mafe.TinyCsv 2.1.0**     | **100**     |        **71.76 μs** |      **0.662 μs** |      **1.177 μs** |        **71.67 μs** |     **4.6387** |        **-** |        **-** |     **119.25 KB** |
| SaveToText   | Job-DLAJMG | Mafe.TinyCsv 2.1.0     | 100     |        92.13 μs |      0.705 μs |      0.659 μs |        92.28 μs |     4.2725 |   0.1221 |        - |     107.36 KB |
| LoadFromText | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 100     |       106.40 μs |      0.703 μs |      0.657 μs |       106.50 μs |     7.9346 |   0.3662 |        - |     195.05 KB |
| SaveToText   | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 100     |        93.07 μs |      0.706 μs |      0.589 μs |        92.95 μs |     4.2725 |   0.1221 |        - |     107.36 KB |
| LoadFromText | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 100     |        64.78 μs |      0.571 μs |      0.534 μs |        64.76 μs |     3.7842 |   0.1221 |        - |      94.48 KB |
| SaveToText   | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 100     |       100.22 μs |      0.588 μs |      0.550 μs |       100.37 μs |     5.0049 |   0.2441 |        - |     123.86 KB |
| **LoadFromText** | **Job-DLAJMG** | **Mafe.TinyCsv 2.1.0**     | **1000**    |       **726.58 μs** |      **6.241 μs** |      **5.533 μs** |       **726.38 μs** |    **46.8750** |  **10.7422** |        **-** |    **1169.54 KB** |
| SaveToText   | Job-DLAJMG | Mafe.TinyCsv 2.1.0     | 1000    |     1,117.35 μs |     21.974 μs |     21.582 μs |     1,114.47 μs |    41.0156 |  41.0156 |  41.0156 |    1127.94 KB |
| LoadFromText | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 1000    |     1,066.91 μs |     20.474 μs |     21.907 μs |     1,052.81 μs |    78.1250 |  17.5781 |        - |     1934.4 KB |
| SaveToText   | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 1000    |     1,120.37 μs |     22.142 μs |     20.712 μs |     1,111.79 μs |    41.0156 |  41.0156 |  41.0156 |    1127.94 KB |
| LoadFromText | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 1000    |       646.99 μs |     12.368 μs |     16.082 μs |       641.23 μs |    34.1797 |   9.7656 |        - |     842.42 KB |
| SaveToText   | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 1000    |     1,105.48 μs |     20.658 μs |     19.324 μs |     1,096.48 μs |    82.0313 |  41.0156 |  41.0156 |    1292.09 KB |
| **LoadFromText** | **Job-DLAJMG** | **Mafe.TinyCsv 2.1.0**     | **10000**   |     **8,391.06 μs** |     **96.125 μs** |     **89.916 μs** |     **8,381.17 μs** |   **484.3750** | **187.5000** | **187.5000** |   **11672.54 KB** |
| SaveToText   | Job-DLAJMG | Mafe.TinyCsv 2.1.0     | 10000   |    11,880.59 μs |    238.608 μs |    645.090 μs |    11,788.52 μs |   484.3750 | 484.3750 | 484.3750 |   10724.84 KB |
| LoadFromText | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 10000   |    14,620.01 μs |  1,187.007 μs |  3,499.918 μs |    11,839.71 μs |   781.2500 | 187.5000 | 187.5000 |   19328.02 KB |
| SaveToText   | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 10000   |    11,465.53 μs |    224.331 μs |    209.839 μs |    11,465.43 μs |   484.3750 | 484.3750 | 484.3750 |   10724.84 KB |
| LoadFromText | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 10000   |     7,421.20 μs |     65.251 μs |     57.843 μs |     7,437.94 μs |   390.6250 | 195.3125 | 195.3125 |    8321.98 KB |
| SaveToText   | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 10000   |    12,713.09 μs |    250.308 μs |    288.255 μs |    12,648.92 μs |   500.0000 | 484.3750 | 484.3750 |   12365.56 KB |
| **LoadFromText** | **Job-DLAJMG** | **Mafe.TinyCsv 2.1.0**     | **100000**  |    **74,360.47 μs** |    **531.274 μs** |    **470.961 μs** |    **74,297.41 μs** |  **4428.5714** |        **-** |        **-** |  **116701.82 KB** |
| SaveToText   | Job-DLAJMG | Mafe.TinyCsv 2.1.0     | 100000  |    94,426.85 μs |  1,167.166 μs |  1,091.768 μs |    94,018.60 μs |  3500.0000 |        - |        - |  103107.82 KB |
| LoadFromText | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 100000  |   110,603.39 μs |    988.374 μs |    876.168 μs |   110,215.62 μs |  7400.0000 |        - |        - |  193263.58 KB |
| SaveToText   | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 100000  |    95,829.55 μs |    596.744 μs |    498.308 μs |    95,977.35 μs |  3500.0000 |        - |        - |  103107.82 KB |
| LoadFromText | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 100000  |    71,489.27 μs |  1,054.027 μs |    985.937 μs |    71,504.73 μs |  3000.0000 | 125.0000 | 125.0000 |   83116.93 KB |
| SaveToText   | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 100000  |   104,370.81 μs |  1,892.284 μs |  3,907.895 μs |   102,658.48 μs |  4000.0000 |        - |        - |   119514.1 KB |
| **LoadFromText** | **Job-DLAJMG** | **Mafe.TinyCsv 2.1.0**     | **1000000** |   **772,174.89 μs** |  **5,726.903 μs** |  **5,076.750 μs** |   **771,846.60 μs** | **44000.0000** |        **-** |        **-** |  **1166994.8 KB** |
| SaveToText   | Job-DLAJMG | Mafe.TinyCsv 2.1.0     | 1000000 |   988,123.33 μs |  7,287.623 μs |  6,816.847 μs |   985,134.60 μs | 35000.0000 |        - |        - | 1129336.53 KB |
| LoadFromText | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 1000000 | 1,098,930.90 μs | 11,893.115 μs | 11,124.827 μs | 1,099,905.90 μs | 75000.0000 |        - |        - | 1932619.03 KB |
| SaveToText   | Job-BHLBDE | Mafe.TinyCsv 2.2.2     | 1000000 |   983,407.67 μs |  4,735.031 μs |  3,953.965 μs |   983,659.10 μs | 35000.0000 |        - |        - | 1129336.53 KB |
| LoadFromText | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 1000000 |   679,338.99 μs |  8,184.794 μs |  7,656.062 μs |   677,154.60 μs | 30000.0000 |        - |        - |  831066.12 KB |
| SaveToText   | Job-JWNPKE | Mafe.TinyCsv 3.0.0-rc1 | 1000000 | 1,106,889.53 μs |  8,664.685 μs |  8,104.952 μs | 1,107,780.40 μs | 42000.0000 |        - |        - | 1293399.17 KB |
