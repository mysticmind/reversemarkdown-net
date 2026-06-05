using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Markdig;
using Xunit;
using Xunit.Abstractions;

namespace ReverseMarkdown.Test
{
    // Measurement (not a gate): how does the v6 Markdown DOM CommonMark path roundtrip the spec?
    public class CommonMarkV6MeasureTests
    {
        private readonly ITestOutputHelper _output;

        public CommonMarkV6MeasureTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Measure_v6_commonmark_roundtrip()
        {
            var path = SpecPath();
            if (!File.Exists(path))
            {
                _output.WriteLine("spec file missing");
                return;
            }

            var examples = JsonSerializer.Deserialize<List<SpecExample>>(
                File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            var converter = new Converter(new Config { UseMarkdownDom = true, Flavor = Config.MarkdownFlavor.CommonMark });
            var pipeline = new MarkdownPipelineBuilder().Build();

            var pass = new Dictionary<string, int>();
            var total = new Dictionary<string, int>();
            var samples = new List<string>();
            var overallPass = 0;
            var overallTotal = 0;

            foreach (var ex in examples)
            {
                if (string.IsNullOrEmpty(ex.Html))
                {
                    continue;
                }

                total[ex.Section] = total.GetValueOrDefault(ex.Section) + 1;
                overallTotal++;

                string actual;
                try
                {
                    actual = Norm(Markdown.ToHtml(converter.Convert(ex.Html), pipeline));
                }
                catch (Exception e)
                {
                    actual = "THREW: " + e.GetType().Name;
                }

                if (actual == Norm(ex.Html))
                {
                    pass[ex.Section] = pass.GetValueOrDefault(ex.Section) + 1;
                    overallPass++;
                }
                else if (samples.Count < 25)
                {
                    samples.Add($"[{ex.Section} #{ex.Example}] in={Inline(ex.Html)}\n   exp={Inline(Norm(ex.Html))}\n   got={Inline(actual)}");
                }
            }

            var rate = (double)overallPass / overallTotal;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"OVERALL v6 CommonMark roundtrip: {overallPass}/{overallTotal} = {100.0 * rate:F1}%");
            foreach (var section in total.Keys.OrderByDescending(s => total[s]))
            {
                sb.AppendLine($"  {pass.GetValueOrDefault(section),4}/{total[section],-4} {section}");
            }

            foreach (var s in samples)
            {
                sb.AppendLine(s);
            }

            _output.WriteLine(sb.ToString());

            // Regression gate: lock in current progress (94.5%). Raise as the writer improves.
            Assert.True(rate >= 0.94, $"v6 CommonMark roundtrip regressed to {100.0 * rate:F1}%\n{sb}");
        }

        private static string Inline(string s) => s.Replace("\r\n", "\\n").Replace("\n", "\\n");

        private static string Norm(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }

            var n = html.Replace("\r\n", "\n").TrimEnd();
            n = n.Replace("<br>", "<br />").Replace("<br/>", "<br />").Replace("<hr>", "<hr />").Replace("<hr/>", "<hr />");
            n = n.Replace("&#10;", "\n").Replace("&#xA;", "\n");
            n = System.Text.RegularExpressions.Regex.Replace(n, @">\s+<", "><");
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(n);
            return doc.DocumentNode.InnerHtml.Replace(" ", " ");
        }

        private static string SpecPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ReverseMarkdown.Test.csproj")))
            {
                dir = dir.Parent;
            }

            return Path.Combine(dir!.FullName, "TestData", "commonmark.json");
        }

        private sealed class SpecExample
        {
            public int Example { get; set; }
            public string Section { get; set; } = string.Empty;
            public string Html { get; set; } = string.Empty;
        }
    }
}
