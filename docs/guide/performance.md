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

## 6.3.0: real-world pages

The v5 vs v6 comparison above uses synthetic paragraph fixtures. Version 6.3.0 was measured on a
corpus of real pages instead (Wikipedia articles, framework documentation and MDN pages from the
[mdream](https://github.com/harlan-zw/mdream) project), which is where it found its main fix: the
cleanup pass that handles unclosed `<script>` and `<style>` tags copied the rest of the document once
per tag, so pages with many of them paid for it on every conversion. That pass is now linear.

![ReverseMarkdown 6.2.1 vs 6.3.0: percentage change per real-world page](/benchmark-6.2.1-6.3.0.svg)

Published 6.2.1 against 6.3.0, default flavor, .NET 9.0 on an Apple M1. Lower is better.

| Fixture                       | Size    | Mean time 6.2.1 → 6.3.0       | Allocated 6.2.1 → 6.3.0         |
| ----------------------------- | ------: | ----------------------------- | ------------------------------- |
| nuxt-example.html             | 3.5 KB  | 80.8 → **68.2 μs** (-16%)     | 133.9 → **105.6 KB** (-21%)     |
| vuejs-docs.html               | 110 KB  | 4.92 → **4.79 ms** (-3%)      | 4.82 → **3.30 MB** (-32%)       |
| wikipedia-small.html          | 162 KB  | 7.94 → **8.08 ms** (+2%)      | 7.70 → **6.19 MB** (-20%)       |
| mdn-array.html                | 230 KB  | 12.61 → **10.37 ms** (-18%)   | 17.68 → **8.01 MB** (-55%)      |
| react-learn.html              | 259 KB  | 11.40 → **10.82 ms** (-5%)    | 13.99 → **6.97 MB** (-50%)      |
| github-markdown-complete.html | 420 KB  | 13.30 → **10.56 ms** (-21%)   | 24.68 → **10.48 MB** (-58%)     |
| wikipedia-largest.html        | 1.77 MB | 198.3 → **189.9 ms** (-4%)    | 222.5 → **198.6 MB** (-11%)     |

Allocations drop on every page, by about half on the script-heavy ones. Time changes under about 5%
are within run-to-run noise, so read those pages as unchanged in speed; the allocation figures are
deterministic. 6.3.0 also adds [TextWriter overloads](/guide/getting-started#writing-to-a-textwriter),
which lower peak memory further by never building the output as one string.

## Reproducing the benchmarks

The benchmark lives in `src/ReverseMarkdown.Benchmark`. By default it references the published NuGet
package, so you can swap the version between runs to compare releases:

```bash
# v6 (default)
dotnet run -c Release --project src/ReverseMarkdown.Benchmark

# v5, for comparison
dotnet run -c Release --project src/ReverseMarkdown.Benchmark -p:RMVersion=5.5.0
```

To measure your local working tree instead of a published package, which is what you want when
checking whether a change helped, add `-p:UseLocalReverseMarkdown=true`.

The real-world corpus is third-party content, so it is not committed. Fetch it once, then run either
a quick throughput summary or the full BenchmarkDotNet run:

```bash
./scripts/fetch-benchmark-corpus.sh

# Quick summary in MB/s of input HTML, per size group (takes seconds)
dotnet run -c Release --project src/ReverseMarkdown.Benchmark -- throughput

# Full BenchmarkDotNet run over the corpus, with allocations per fixture
dotnet run -c Release --project src/ReverseMarkdown.Benchmark -- --filter '*Corpus*'
```

Numbers vary with hardware and runtime; compare runs made on the same machine.
