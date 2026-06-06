using BenchmarkDotNet.Running;
using ReverseMarkdown.Benchmark;

BenchmarkSwitcher.FromAssembly(typeof(CompareBenchmark).Assembly).Run(args);
