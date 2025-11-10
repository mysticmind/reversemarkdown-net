using BenchmarkDotNet.Running;
using ReverseMarkdown.Benchmark;


var summary = BenchmarkRunner.Run<CompareBenchmark>();
Console.ReadLine();
