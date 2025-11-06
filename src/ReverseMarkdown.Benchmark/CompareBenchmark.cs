using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;


namespace ReverseMarkdown.Benchmark;

[SimpleJob(RuntimeMoniker.Net90)]
[RPlotExporter]
[MemoryDiagnoser]
public class CompareBenchmark {
    private string _hugeHtml = null!;
    private Converter _converter = null!;

    [GlobalSetup]
    public void Setup()
    {
        _hugeHtml = FileHelper.ReadFile("Files/huge.html");
        _converter = new Converter(new Config());
    }

    [Benchmark]
    public string ReverseMarkdown()
    {
        var result = _converter.Convert(_hugeHtml);
        return result;
    }
}
