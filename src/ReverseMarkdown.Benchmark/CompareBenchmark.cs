using BenchmarkDotNet.Attributes;

namespace ReverseMarkdown.Benchmark;

// Benchmarks the published ReverseMarkdown package (version set in the csproj / -p:RMVersion).
// The Convert(string) API is identical across v5 and v6, so the same benchmark runs for both;
// run once per version and compare the results.
[MemoryDiagnoser]
public class CompareBenchmark
{
    private string _html = null!;
    private ReverseMarkdown.Converter _converter = null!;

    [Params(
        "Files/1000-paragraphs.html",
        "Files/10k-paragraphs.html",
        "Files/huge.html")]
    public string FileName { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        _html = FileHelper.ReadFile(FileName);
        _converter = new ReverseMarkdown.Converter(new ReverseMarkdown.Config());
    }

    [Benchmark]
    public string Convert()
    {
        return _converter.Convert(_html);
    }
}
