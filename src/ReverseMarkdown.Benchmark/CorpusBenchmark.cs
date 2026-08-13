using BenchmarkDotNet.Attributes;

namespace ReverseMarkdown.Benchmark;

/// <summary>
/// Converts the real-world corpus fetched by <c>scripts/fetch-benchmark-corpus.sh</c> - Wikipedia
/// articles, framework documentation and MDN pages, which exercise the converter far more
/// realistically than the synthetic paragraph fixtures.
/// </summary>
/// <remarks>
/// Fixtures are grouped by size so a regression can be attributed to a size class rather than lost
/// in an average. Run with <c>--filter '*Corpus*'</c>.
/// </remarks>
[MemoryDiagnoser]
[CategoriesColumn]
public class CorpusBenchmark
{
    // fixture -> size group.
    private static readonly Dictionary<string, string> Groups = new()
    {
        ["nuxt-example.html"] = "clean_small",                 // < 10 KB
        ["vuejs-docs.html"] = "clean_medium",                  // 10-200 KB
        ["wikipedia-small.html"] = "clean_medium",
        ["mdn-array.html"] = "clean_medium",
        ["react-learn.html"] = "clean_medium",
        ["github-markdown-complete.html"] = "clean_medium",
        ["wikipedia-largest.html"] = "clean_large",            // 200 KB - 2 MB
    };

    private string _html = null!;
    private Converter _converter = null!;

    [ParamsSource(nameof(Fixtures))]
    public string Fixture { get; set; } = null!;

    public static IEnumerable<string> Fixtures => Groups.Keys;

    /// <summary>The size group this fixture belongs to, surfaced as a BenchmarkDotNet category.</summary>
    public string Group => Groups[Fixture];

    [GlobalSetup]
    public void Setup()
    {
        _html = CorpusFiles.Read(Fixture);
        _converter = new Converter(new Config());
    }

    [Benchmark]
    public string Convert() => _converter.Convert(_html);
}
