using BenchmarkDotNet.Running;
using ReverseMarkdown.Benchmark;

// "throughput" prints a quick MB/s summary over the corpus; anything else goes to BenchmarkDotNet.
if (args.Length > 0 && args[0].Equals("throughput", StringComparison.OrdinalIgnoreCase))
{
    return Throughput.Run();
}

BenchmarkSwitcher.FromAssembly(typeof(CompareBenchmark).Assembly).Run(args);
return 0;
