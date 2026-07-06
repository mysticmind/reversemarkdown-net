using BenchmarkDotNet.Attributes;


namespace ReverseMarkdown.Benchmark;

[MemoryDiagnoser]
[BaselineColumn]
public class CompareBenchmark
{
    private string _html = null!;
    private Converter _converter = null!;

    [Params(
        "Files/1000-paragraphs.html",
        "Files/10k-paragraphs.html",
        "Files/huge.html")]
    public string FileName { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        _html = FileHelper.ReadFile(FileName);
        _converter = new Converter(new ReverseMarkdown.Config());
    }

    [Benchmark(Baseline = true)]
    public string V6_AngleSharpMarkdownDom()
    {
        return _converter.Convert(_html);
    }
}
