# Benchmark Results for ReverseMarkdown


**Summary**: Running with `Files/1000-paragraphs.html` file (size: 442KB) and `1000` paragraphs.

```
BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.6899)
AMD Ryzen 9 3900X 3.80GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 9.0.10 (9.0.10, 9.0.1025.47515), X64 RyuJIT x86-64-v3
  .NET 9.0 : .NET 9.0.10 (9.0.10, 9.0.1025.47515), X64 RyuJIT x86-64-v3
```

Job=.NET 9.0  Runtime=.NET 9.0

| Method                     | Mean     | Error    | StdDev   | Gen0        | Gen1       | Gen2       | Allocated | Ratio |
|--------------------------- |---------:|---------:|---------:|------------:|-----------:|-----------:|----------:|------:|
| ReverseMarkdown TextWriter | 25.96 ms | 0.519 ms | 0.744 ms |   3500.0000 |  1593.7500 |  1218.7500 |  24.95 MB |     1 |
| ReverseMarkdown v4.7.1     | 147.7 ms |  2.95 ms |  8.37 ms | 100500.0000 | 97500.0000 | 97500.0000 | 896.35 MB | 5.689 |

**Hints:** Outliers

 - CompareBenchmark.ReverseMarkdown: .NET 9.0 -> 2 outliers were removed (172.08 ms, 174.42 ms)

---

**Summary:** Running with `Files/huge.html` file (size: 16MB) and `41312` paragraphs.

```
BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.6899)
AMD Ryzen 9 3900X 3.80GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 9.0.10 (9.0.10, 9.0.1025.47515), X64 RyuJIT x86-64-v3
  .NET 9.0 : .NET 9.0.10 (9.0.10, 9.0.1025.47515), X64 RyuJIT x86-64-v3
```

Job=.NET 9.0  Runtime=.NET 9.0

| Method                     | Mean      | Error    | StdDev   | Median     | Gen0         | Gen1         | Gen2         | Allocated    | Ratio  |
|--------------------------- |----------:|---------:|---------:|-----------:|-------------:|-------------:|-------------:|-------------:|-------:|
| ReverseMarkdown TextWriter |   0.944 s | 0.0175 s | 0.0172 s |          - |   86000.0000 |   20000.0000 |    3000.0000 |    955.34 MB |      1 |
| ReverseMarkdown v4.7.1     | 191.611 s | 3.7686 s | 4.4863 s | 191.8565 s | 2735000.0000 | 2666000.0000 | 2659000.0000 | 640544.94 MB | 202.97 |

**Legends**

```
  Mean      : Arithmetic mean of all measurements
  Error     : Half of 99.9% confidence interval
  StdDev    : Standard deviation of all measurements
  Median    : Value separating the higher half of all measurements (50th percentile)
  Gen0      : GC Generation 0 collects per 1000 operations
  Gen1      : GC Generation 1 collects per 1000 operations
  Gen2      : GC Generation 2 collects per 1000 operations
  Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Ratio     : Mean of the method divided by the mean of the baseline
  1 s       : 1 Second (1 sec)
```
