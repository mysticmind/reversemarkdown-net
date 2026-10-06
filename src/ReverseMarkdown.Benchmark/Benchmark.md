# Benchmark Results for ReverseMarkdown

## Running the benchmarks

```bash
# One-time: fetch the real-world corpus (third-party fixtures, gitignored)
./scripts/fetch-benchmark-corpus.sh

# Quick throughput summary in MB/s of input HTML, per size group (seconds to run)
dotnet run -c Release --project src/ReverseMarkdown.Benchmark -- throughput

# Full BenchmarkDotNet run over the corpus, with allocations per fixture
dotnet run -c Release --project src/ReverseMarkdown.Benchmark -- --filter '*Corpus*'
```

By default the benchmark project measures the **published** ReverseMarkdown package, which is what
you want for release-to-release comparisons (`-p:RMVersion=5.5.0`). To measure your working tree
instead - the usual case when checking whether a change helped - add:

```bash
-p:UseLocalReverseMarkdown=true
```

### The corpus

`scripts/fetch-benchmark-corpus.sh` downloads the fixtures the mdream project publishes: Wikipedia
articles, framework documentation and MDN pages, which exercise the converter far more realistically
than synthetic input. They are grouped by size (`clean_small`, `clean_medium`, `clean_large`) so a
regression can be attributed to a size class instead of being averaged away.

The corpus is third-party content and is not committed; the directory is gitignored.

## Real-world corpus: 6.2.1 vs 6.3.0

The published 6.2.1 package against the 6.3.0 working tree (`-p:RMVersion=6.2.1` and
`-p:UseLocalReverseMarkdown=true`), default flavor, `--filter '*Corpus*'`. Lower is better.

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 10.0.101
  [Host]           : .NET 9.0.0 (9.0.0, 9.0.24.52809), Arm64 RyuJIT armv8.0-a
  LibraryUnderTest : .NET 9.0.0 (9.0.0, 9.0.24.52809), Arm64 RyuJIT armv8.0-a
```

| Fixture                       | Group        | Size    | Mean 6.2.1 | Mean 6.3.0 | Change | Allocated 6.2.1 | Allocated 6.3.0 | Change |
|------------------------------ |------------- |--------:|-----------:|-----------:|-------:|----------------:|----------------:|-------:|
| nuxt-example.html             | clean_small  |  3.5 KB |   80.81 μs |   68.18 μs |   -16% |        133.9 KB |        105.6 KB |   -21% |
| vuejs-docs.html               | clean_medium |  110 KB |   4.915 ms |   4.785 ms |    -3% |         4.82 MB |         3.30 MB |   -32% |
| wikipedia-small.html          | clean_medium |  162 KB |   7.936 ms |   8.082 ms |    +2% |         7.70 MB |         6.19 MB |   -20% |
| mdn-array.html                | clean_medium |  230 KB |  12.608 ms |  10.370 ms |   -18% |        17.68 MB |         8.01 MB |   -55% |
| react-learn.html              | clean_medium |  259 KB |  11.399 ms |  10.818 ms |    -5% |        13.99 MB |         6.97 MB |   -50% |
| github-markdown-complete.html | clean_medium |  420 KB |  13.304 ms |  10.555 ms |   -21% |        24.68 MB |        10.48 MB |   -58% |
| wikipedia-largest.html        | clean_large  | 1.77 MB | 198.341 ms | 189.931 ms |    -4% |       222.51 MB |       198.64 MB |   -11% |

The whole difference is the script/style cleanup pass, which used to copy the rest of the document
once per `<script>` or `<style>` open tag. Pages with many of those tags gain the most: allocations
roughly halve on `mdn-array`, `react-learn` and `github-markdown-complete`. Time changes under about
5% are within run-to-run noise on this machine (the 99.9% confidence interval is around 2% of the
mean), so `vuejs-docs`, `wikipedia-small`, `react-learn` and `wikipedia-largest` should be read as
unchanged in speed; the allocation figures are deterministic.

---

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
