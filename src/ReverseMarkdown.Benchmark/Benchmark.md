# Benchmark Results for ReverseMarkdown

**Legends**

```
  Mean      : Arithmetic mean of all measurements
  Error     : Half of 99.9% confidence interval
  StdDev    : Standard deviation of all measurements
  Gen0      : GC Generation 0 collects per 1000 operations
  Gen1      : GC Generation 1 collects per 1000 operations
  Gen2      : GC Generation 2 collects per 1000 operations
  Allocated : Allocated memory per single operation (managed only, inclusive, 1KB = 1024B)
  Ratio     : Mean of the method divided by the mean of the baseline
  1 s       : 1 Second (1 sec)
  1 ms      : 1 Millisecond (1 ms)
```

---

## Comparing ReverseMarkdown v4.7.1 vs TextWriter approach

**Hardware: AMD Ryzen 9 3900X 3.80GHz, 24 cores, 12 physical cores**

```
BenchmarkDotNet v0.15.6, Windows 11 (10.0.26200.6899)
AMD Ryzen 9 3900X 3.80GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 9.0.306
  [Host]   : .NET 9.0.10 (9.0.10, 9.0.1025.47515), X64 RyuJIT x86-64-v3
  .NET 9.0 : .NET 9.0.10 (9.0.10, 9.0.1025.47515), X64 RyuJIT x86-64-v3
```

Job=.NET 9.0  Runtime=.NET 9.0

### Running with `Files/1000-paragraphs.html` file (size: 442KB) and `1000` paragraphs.

| Method                     | Mean     | Error    | StdDev   | Gen0        | Gen1       | Gen2       | Allocated | Ratio |
|--------------------------- |---------:|---------:|---------:|------------:|-----------:|-----------:|----------:|------:|
| ReverseMarkdown TextWriter | 25.96 ms | 0.519 ms | 0.744 ms |   3500.0000 |  1593.7500 |  1218.7500 |  24.95 MB |     1 |
| ReverseMarkdown v4.7.1     | 147.7 ms |  2.95 ms |  8.37 ms | 100500.0000 | 97500.0000 | 97500.0000 | 896.35 MB | 5.689 |

Outliers:
- ReverseMarkdown v4.7.1: .NET 9.0 -> 2 outliers were removed (172.08 ms, 174.42 ms)

### Running with `Files/10k-paragraphs.html` file (size: 3.7MB) and `10k` paragraphs.

| Method                     | Mean     | Error   | StdDev  | Gen0        | Gen1        | Gen2        | Allocated | Ratio |
|--------------------------- |---------:|--------:|--------:|------------:|------------:|------------:|----------:|------:|
| ReverseMarkdown TextWriter |  0.232 s | 0.005 s | 0.009 s |  20000.0000 |   5000.0000 |   1000.0000 | 210.15 MB |     1 |
| ReverseMarkdown v4.7.1     |  14.08 s | 0.280 s | 0.747 s | 624000.0000 | 605000.0000 | 603000.0000 |  75.27 GB | 60.69 |

Outliers:
- ReverseMarkdown TextWriter: .NET 9.0 -> 1 outlier  was  removed (265.53 ms)
- ReverseMarkdown v4.7.1: .NET 9.0 -> 2 outliers were removed (17.04 s, 17.23 s)

### Running with `Files/huge.html` file (size: 16MB) and `41312` paragraphs.

| Method                     | Mean      | Error    | StdDev   | Gen0         | Gen1         | Gen2         | Allocated    | Ratio  |
|--------------------------- |----------:|---------:|---------:|-------------:|-------------:|-------------:|-------------:|-------:|
| ReverseMarkdown TextWriter |   0.944 s | 0.0175 s | 0.0172 s |   86000.0000 |   20000.0000 |    3000.0000 |    955.34 MB |      1 |
| ReverseMarkdown v4.7.1     | 191.611 s | 3.7686 s | 4.4863 s | 2735000.0000 | 2666000.0000 | 2659000.0000 | 640544.94 MB | 202.97 |
