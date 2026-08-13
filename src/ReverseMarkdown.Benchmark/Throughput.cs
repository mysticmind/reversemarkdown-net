using System.Diagnostics;
using System.Text;

namespace ReverseMarkdown.Benchmark;

/// <summary>
/// A quick throughput summary over the corpus, reported in MB/s of input HTML. BenchmarkDotNet gives
/// the rigorous per-fixture numbers; this trades some rigour for running in seconds rather than
/// minutes, which makes it usable while iterating on a change.
/// </summary>
public static class Throughput
{
    public static int Run()
    {
        var fixtures = CorpusBenchmark.Fixtures.ToArray();
        var missing = fixtures.Where(f => !CorpusFiles.Exists(f)).ToArray();
        if (missing.Length > 0)
        {
            Console.Error.WriteLine(CorpusFiles.FetchHint);
            Console.Error.WriteLine("Missing: " + string.Join(", ", missing));
            return 1;
        }

        var converter = new Converter(new Config());
        Console.WriteLine($"runtime {Environment.Version}, server GC={System.Runtime.GCSettings.IsServerGC}, " +
                          $"cores={Environment.ProcessorCount}");
        Console.WriteLine();
        Console.WriteLine("fixture                              KB       ms     MB/s   best MB/s      group");

        var byGroup = new Dictionary<string, List<double>>();
        foreach (var fixture in fixtures)
        {
            var html = CorpusFiles.Read(fixture);
            var bytes = Encoding.UTF8.GetByteCount(html);
            var megabytes = bytes / 1024.0 / 1024.0;

            for (var i = 0; i < 5; i++)
            {
                converter.Convert(html);   // warm up JIT and caches
            }

            var iterations = bytes < 50_000 ? 500 : bytes < 500_000 ? 60 : 20;
            var best = double.MaxValue;
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                var start = Stopwatch.GetTimestamp();
                converter.Convert(html);
                var elapsed = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                if (elapsed < best)
                {
                    best = elapsed;
                }
            }

            watch.Stop();

            var meanMs = watch.Elapsed.TotalMilliseconds / iterations;
            var mbs = megabytes / (meanMs / 1000.0);
            var group = new CorpusBenchmark { Fixture = fixture }.Group;

            Console.WriteLine($"{fixture,-32}{bytes / 1024.0,8:F1}{meanMs,9:F2}{mbs,9:F1}{megabytes / (best / 1000.0),12:F1}" +
                              $"   {group}");

            if (!byGroup.TryGetValue(group, out var list))
            {
                byGroup[group] = list = new List<double>();
            }

            list.Add(mbs);
        }

        Console.WriteLine();
        foreach (var (group, values) in byGroup.OrderBy(g => g.Key))
        {
            Console.WriteLine($"  {group,-16}{values.Min(),7:F1} - {values.Max(),5:F1} MB/s   (n={values.Count})");
        }

        var all = byGroup.Values.SelectMany(v => v).ToArray();
        Console.WriteLine($"  {"overall",-16}{all.Min(),7:F1} - {all.Max(),5:F1} MB/s");
        return 0;
    }
}
