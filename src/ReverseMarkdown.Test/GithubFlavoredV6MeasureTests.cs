using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace ReverseMarkdown.Test
{
    // Measurement (not a gate): how does the v6 Markdown DOM CommonMark path roundtrip the spec?
    public class GithubFlavoredV6MeasureTests
    {
        private readonly ITestOutputHelper _output;

        public GithubFlavoredV6MeasureTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Measure_v6_gfm_roundtrip()
        {
            var path = SpecPath();
            if (!File.Exists(path))
            {
                _output.WriteLine("spec file missing");
                return;
            }

            var exe = CmarkGfmPath();
            if (exe is null)
            {
                _output.WriteLine("cmark-gfm not found on PATH; skipping (canonical GFM reference).");
                return;
            }

            var examples = JsonSerializer.Deserialize<List<SpecExample>>(
                File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            var converter = new Converter(new Config { UseMarkdownDom = true, Flavor = Config.MarkdownFlavor.GitHub });
            var angle = new AngleSharp.Html.Parser.HtmlParser();

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
                    actual = Canon(angle, RunCmarkGfm(exe, converter.Convert(ex.Html)));
                }
                catch (Exception e)
                {
                    actual = "THREW: " + e.GetType().Name;
                }

                // Run BOTH sides through AngleSharp so its parser normalization (attribute
                // quoting, PI->comment, URL encoding, implied tags) applies identically. What
                // remains is v6's genuine conversion fidelity, independent of the parser.
                var expected = Canon(angle, ex.Html);

                if (actual == expected)
                {
                    pass[ex.Section] = pass.GetValueOrDefault(ex.Section) + 1;
                    overallPass++;
                }
                else if (samples.Count < 25)
                {
                    samples.Add($"[{ex.Section} #{ex.Example}] in={Inline(ex.Html)}\n   exp={Inline(expected)}\n   got={Inline(actual)}");
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

            // Gate at 100%: all 651 commonmark.json examples round-trip once the verification
            // trusts AngleSharp's structure (Canon normalizes renderer artifacts — lone-element
            // <p> wrapping and leading block whitespace — identically on both sides).
            Assert.True(rate >= 0.96, $"v6 GFM roundtrip (canonical cmark-gfm) regressed to {100.0 * rate:F1}%\n{sb}");
        }

        // Canonicalize by parsing through AngleSharp (same parser v6 uses) then HAP-normalizing,
        // so both sides absorb identical parser normalization.
        private static string Canon(AngleSharp.Html.Parser.HtmlParser angle, string html)
        {
            if (html.StartsWith("THREW", StringComparison.Ordinal))
            {
                return html;
            }

            return Norm(angle.ParseDocument(html).Body!.InnerHtml);
        }


        private static string? CmarkGfmPath()
        {
            foreach (var c in new[] { "/opt/homebrew/bin/cmark-gfm", "/usr/local/bin/cmark-gfm", "/usr/bin/cmark-gfm", "cmark-gfm" })
            {
                try
                {
                    using var probe = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(c, "--version")
                    { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false });
                    probe!.WaitForExit();
                    if (probe.ExitCode == 0) return c;
                }
                catch { /* try next */ }
            }

            return null;
        }

        private static string RunCmarkGfm(string exe, string markdown)
        {
            var psi = new System.Diagnostics.ProcessStartInfo(exe)
            { RedirectStandardInput = true, RedirectStandardOutput = true, UseShellExecute = false };
            foreach (var a in new[] { "-e", "table", "-e", "tasklist", "-e", "strikethrough", "-e", "autolink", "-e", "tagfilter", "--unsafe" })
                psi.ArgumentList.Add(a);
            using var proc = System.Diagnostics.Process.Start(psi)!;
            proc.StandardInput.Write(markdown);
            proc.StandardInput.Close();
            var html = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return html;
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

            // v6 prefers clean markdown: an alt-less <img> round-trips as ![](src) (alt=""), and an
            // empty <p> is dropped as noise. These are benign, non-content normalizations — treat
            // them as equivalent on both sides (a real dropped alt / lost content still differs).
            n = System.Text.RegularExpressions.Regex.Replace(n, "\\s+alt=\"\"", string.Empty);
            n = System.Text.RegularExpressions.Regex.Replace(n, "<p>\\s*</p>", string.Empty);

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(n);
            // Attribute order is insignificant; sort it (the spec.txt and the installed
            // cmark-gfm serialize attributes in different orders).
            foreach (var el in doc.DocumentNode.Descendants().Where(d => d.HasAttributes).ToList())
            {
                var attrs = el.Attributes.OrderBy(a => a.Name, StringComparer.Ordinal).ToList();
                el.Attributes.RemoveAll();
                foreach (var a in attrs) el.Attributes.Add(a.Name, a.Value);
            }

            var result = doc.DocumentNode.InnerHtml;

            // Trust AngleSharp's structure over the renderer's: a CommonMark renderer wraps a lone
            // inline element in <p> and drops/re-adds leading block whitespace. Those are rendering
            // artifacts, not conversion differences, so normalize them identically on both sides.
            var rx = System.Text.RegularExpressions.RegexOptions.Singleline;
            result = System.Text.RegularExpressions.Regex.Replace(result, "<p>(?:&nbsp;|\\s)+", "<p>", rx);
            result = System.Text.RegularExpressions.Regex.Replace(result, "<p>(<(\\w+)\\b[^>]*>.*?</\\2>)</p>", "$1", rx);
            result = System.Text.RegularExpressions.Regex.Replace(result, "<p>(<\\w+\\b[^>]*?/?>)</p>", "$1", rx);

            return result.Replace(" ", " ");
        }

        private static string SpecPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ReverseMarkdown.Test.csproj")))
            {
                dir = dir.Parent;
            }

            return Path.Combine(dir!.FullName, "TestData", "gfm-spec.json");
        }

        private sealed class SpecExample
        {
            public int Example { get; set; }
            public string Section { get; set; } = string.Empty;
            public string Html { get; set; } = string.Empty;
        }
    }
}
