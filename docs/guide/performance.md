# Performance

Version 6 replaces the HtmlAgilityPack-based v5 engine with an [AngleSharp](https://anglesharp.io/)
HTML5 parser feeding a purpose-built Markdown DOM and per-flavor writers. Beyond being more
correct, the new pipeline is substantially faster and allocates far less memory.

![ReverseMarkdown v5 vs v6 performance](/benchmark-v5-v6.png)

## Results

Measured with [BenchmarkDotNet](https://benchmarkdotnet.org/) on .NET 9.0 (Apple M1), comparing
the published `ReverseMarkdown` **5.5.0** (HtmlAgilityPack) against **6.0.0** (AngleSharp +
Markdown DOM) on the same inputs. Lower is better.

| Fixture         | Mean time v5 → v6      | Allocated v5 → v6      |
| --------------- | ---------------------- | ---------------------- |
| 1000 paragraphs | 13.7 → **5.2 ms** (2.6× faster)  | 21 → **7 MB** (2.8× less)  |
| 10k paragraphs  | 119.1 → **63.1 ms** (1.9× faster) | 183 → **67 MB** (2.7× less) |
| huge.html       | 540.6 → **244.3 ms** (2.2× faster) | 779 → **288 MB** (2.7× less) |

Across the board v6 runs roughly **2–2.6× faster** and allocates about **2.7× less memory**. The
memory reduction is the bigger practical win: less GC pressure keeps throughput steady when
converting large documents or many documents concurrently.

## Reproducing the benchmark

The benchmark lives in `src/ReverseMarkdown.Benchmark` and references the published NuGet package,
so you can swap the version between runs to compare releases:

```bash
# v6 (default)
dotnet run -c Release --project src/ReverseMarkdown.Benchmark

# v5, for comparison
dotnet run -c Release --project src/ReverseMarkdown.Benchmark -p:RMVersion=5.5.0
```

Numbers vary with hardware and runtime; the relative gap between v5 and v6 is the point.
