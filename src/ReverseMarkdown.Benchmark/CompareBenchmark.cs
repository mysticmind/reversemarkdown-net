using BenchmarkDotNet.Attributes;


namespace ReverseMarkdown.Benchmark;

[MemoryDiagnoser]
[BaselineColumn]
public class CompareBenchmark
{
    private string _html = null!;
    private Converter _v5 = null!;
    private Converter _v6 = null!;

    [Params(
        "Files/1000-paragraphs.html",
        "Files/10k-paragraphs.html",
        "Files/huge.html")]
    public string FileName { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        _html = FileHelper.ReadFile(FileName);
        _v5 = new Converter(new ReverseMarkdown.Config());
        _v6 = new Converter(new ReverseMarkdown.Config { UseMarkdownDom = true });
    }

    [Benchmark(Baseline = true)]
    public string V5_HtmlAgilityPack()
    {
        return _v5.Convert(_html);
    }

    [Benchmark]
    public string V6_MarkdownDom()
    {
        return _v6.Convert(_html);
    }
}
