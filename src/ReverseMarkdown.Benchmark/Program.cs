using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ReverseMarkdown.Benchmark;

// "throughput" prints a quick MB/s summary over the corpus; anything else goes to BenchmarkDotNet.
if (args.Length > 0 && args[0].Equals("throughput", StringComparison.OrdinalIgnoreCase))
{
    return Throughput.Run();
}

// BenchmarkDotNet rebuilds the project in a child process, which does not see the -p: switches this
// build was given. Forward the ones that pick the library under test, or every run would measure
// the default package regardless of what was asked for.
var metadata = typeof(CompareBenchmark).Assembly
    .GetCustomAttributes<AssemblyMetadataAttribute>()
    .ToDictionary(m => m.Key, m => m.Value ?? string.Empty);

// The package version is irrelevant when the working tree is under test.
var forwarded = metadata.GetValueOrDefault("UseLocalReverseMarkdown") == "true"
    ? new Argument[] { new MsBuildArgument("/p:UseLocalReverseMarkdown=true") }
    : new Argument[] { new MsBuildArgument($"/p:RMVersion={metadata["RMVersion"]}") };

// Named explicitly: an unnamed job takes its id from its arguments, and BenchmarkDotNet uses the id
// as a directory name, which the "/p:" in the argument breaks.
var config = DefaultConfig.Instance.AddJob(new Job("LibraryUnderTest", Job.Default.WithArguments(forwarded)));

Console.WriteLine("ReverseMarkdown under test: " +
                  typeof(ReverseMarkdown.Converter).Assembly.GetName().Version);

BenchmarkSwitcher.FromAssembly(typeof(CompareBenchmark).Assembly).Run(args, config);
return 0;
