using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ReverseMarkdown;
using Xunit;
using Xunit.Abstractions;

namespace ReverseMarkdown.Test
{
    /// <summary>
    /// Dual-run parity harness. Runs the v5 <see cref="Converter.Convert"/>
    /// path and the v6 <c>Render(Parse(...))</c> path over a corpus and classifies each diff.
    /// Per the v6 decision, byte parity is NOT a goal — this harness gates only SEMANTIC
    /// regressions and dropped content, not whitespace. It also prints a progress report.
    /// </summary>
    public class ParityHarnessTests
    {
        private readonly ITestOutputHelper _output;

        public ParityHarnessTests(ITestOutputHelper output)
        {
            _output = output;
        }

        public enum DiffClass
        {
            Identical,
            WhitespaceOnly,
            Semantic,
        }

        // Corpus of standalone snippets. The classification vs v5 is informational; the
        // gate (below) is content-preservation, which applies to every snippet — including
        // not-yet-ported tags (table/sup) that should still survive via bypass/escape-hatch.
        private static readonly string[] Corpus =
        {
            "<h1>Title</h1>",
            "<h2>Sub</h2><p>Body text here.</p>",
            "<p>Para with <strong>bold</strong> and <em>italic</em>.</p>",
            "<p>A <a href=\"https://example.com\">link</a> inline.</p>",
            "<p>An <img src=\"pic.png\" alt=\"alt text\"> image.</p>",
            "<p>Inline <code>code()</code> sample.</p>",
            "<p>Strike <s>removed</s> text.</p>",
            "<ul><li>one</li><li>two</li></ul>",
            "<ol><li>first</li><li>second</li></ol>",
            "<ul><li>top<ul><li>nested</li></ul></li></ul>",
            "<blockquote><p>quoted</p></blockquote>",
            "<pre><code class=\"language-csharp\">var x = 1;</code></pre>",
            "<p>before</p><hr><p>after</p>",

            // Not yet ported — must still preserve content via bypass / escape hatch.
            "<table><tr><th>A</th></tr><tr><td>1</td></tr></table>",
            "<p>super<sup>script</sup></p>",
        };

        // All alphanumeric characters, lowercased, formatting/whitespace stripped. Boundary-
        // insensitive so "super^script^" (v5) and "superscript" (v6) compare as the same content.
        private static string Alnum(string s) =>
            new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        // True when v5's content appears, in order, within v6's content. Allows formatting
        // differences and v6 additions (e.g. a code-fence language); catches genuine drops.
        private static bool PreservesContent(string v5, string v6, out string detail)
        {
            var need = Alnum(v5);
            var hay = Alnum(v6);

            var i = 0;
            foreach (var c in hay)
            {
                if (i < need.Length && need[i] == c)
                {
                    i++;
                }
            }

            detail = i == need.Length ? string.Empty : $"v5='{need}' not preserved in v6='{hay}'";
            return i == need.Length;
        }

        private static DiffClass Classify(string v5, string v6)
        {
            if (string.Equals(v5, v6, StringComparison.Ordinal))
            {
                return DiffClass.Identical;
            }

            return StripWhitespace(v5) == StripWhitespace(v6)
                ? DiffClass.WhitespaceOnly
                : DiffClass.Semantic;
        }

        private static string StripWhitespace(string s) => Regex.Replace(s, @"\s+", string.Empty);

        [Fact]
        public void Report_and_gate_v5_vs_v6_parity()
        {
            var converter = new Converter(new Config());
            var counts = new Dictionary<DiffClass, int>
            {
                [DiffClass.Identical] = 0,
                [DiffClass.WhitespaceOnly] = 0,
                [DiffClass.Semantic] = 0,
            };
            var failures = new List<string>();

            foreach (var html in Corpus)
            {
                var v5 = converter.Convert(html);

                string v6;
                try
                {
                    v6 = converter.Render(converter.Parse(html));
                }
                catch (Exception ex)
                {
                    failures.Add($"THREW    | {html}\n         | {ex.GetType().Name}: {ex.Message}");
                    continue;
                }

                var cls = Classify(v5, v6);
                counts[cls]++;
                _output.WriteLine($"{cls,-14} {html}");
                _output.WriteLine($"   v5: {Inline(v5)}");
                _output.WriteLine($"   v6: {Inline(v6)}");

                // The one strict gate: content may not disappear.
                if (!PreservesContent(v5, v6, out var detail))
                {
                    failures.Add($"DROPPED  | {html}\n         | {detail}");
                }
            }

            _output.WriteLine(string.Empty);
            _output.WriteLine($"vs v5 — Identical: {counts[DiffClass.Identical]}  " +
                              $"WhitespaceOnly: {counts[DiffClass.WhitespaceOnly]}  " +
                              $"Semantic: {counts[DiffClass.Semantic]} (informational)");

            Assert.True(failures.Count == 0,
                "v6 content/throw failures:\n" + string.Join("\n", failures));
        }

        private static string Inline(string s) => s.Replace("\r\n", "\\n").Replace("\n", "\\n");
    }
}
