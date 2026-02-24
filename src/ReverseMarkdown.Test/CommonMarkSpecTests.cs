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
    public class CommonMarkSpecTests
    {
        private readonly ITestOutputHelper _output;

        public CommonMarkSpecTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CommonMark_Spec_Examples_RoundTripHtml()
        {
            var specPath = GetSpecPath();
            Assert.True(
                File.Exists(specPath),
                "CommonMark spec file not found. Download commonmark.json to src/ReverseMarkdown.Test/TestData/commonmark.json"
            );

            var json = File.ReadAllText(specPath);
            var examples = JsonSerializer.Deserialize<List<CommonMarkExample>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
            if (examples == null || examples.Count == 0) {
                throw new InvalidOperationException("CommonMark spec file is empty or invalid.");
            }

            var maxExamples = GetIntEnvironmentVariable("COMMONMARK_MAX_EXAMPLES");
            var selected = maxExamples > 0 ? examples.Take(maxExamples).ToList() : examples;

            var converter = new Converter(new Config { CommonMark = true });
            var pipeline = new MarkdownPipelineBuilder().Build();
            var failures = new List<string>();

            foreach (var example in selected) {
                if (string.IsNullOrEmpty(example.Html)) {
                    continue;
                }

                var markdown = converter.Convert(example.Html);
                var roundTripHtml = Markdown.ToHtml(markdown, pipeline);

                var expected = NormalizeHtml(example.Html);
                var actual = NormalizeHtml(roundTripHtml);

                if (!string.Equals(expected, actual, StringComparison.Ordinal)) {
                    failures.Add(FormatFailure(example, markdown, expected, actual));
                    if (failures.Count >= 10) {
                        break;
                    }
                }
            }

            if (failures.Count > 0) {
                var message = $"CommonMark failures: {failures.Count}/{selected.Count}";
                _output.WriteLine(message);
                foreach (var failure in failures) {
                    _output.WriteLine(failure);
                }

                Assert.Fail(message);
            }
        }

        private static string NormalizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) {
                return string.Empty;
            }

            var normalized = html.Replace("\r\n", "\n").TrimEnd();
            normalized = normalized.Replace("<br>", "<br />");
            normalized = normalized.Replace("<br/>", "<br />");
            normalized = normalized.Replace("<hr>", "<hr />");
            normalized = normalized.Replace("<hr/>", "<hr />");
            normalized = normalized.Replace("&#10;", "\n");
            normalized = normalized.Replace("&#xA;", "\n");
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @">\s+<", "><");

            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(normalized);
            normalized = doc.DocumentNode.InnerHtml;
            normalized = normalized.Replace("\u00A0", " ");

            return normalized;
        }

        private static string FormatFailure(CommonMarkExample example, string markdown, string expected, string actual)
        {
            return $"Example {example.Example} ({example.Section}):\n" +
                   $"Markdown:\n{markdown}\n" +
                   $"Expected HTML:\n{expected}\n" +
                   $"Actual HTML:\n{actual}\n";
        }

        private static int GetIntEnvironmentVariable(string name)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return int.TryParse(value, out var result) ? result : 0;
        }

        private static string GetSpecPath()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ReverseMarkdown.Test.csproj"))) {
                directory = directory.Parent;
            }

            if (directory == null) {
                throw new DirectoryNotFoundException("Could not locate test project directory.");
            }

            return Path.Combine(directory.FullName, "TestData", "commonmark.json");
        }

        private sealed class CommonMarkExample
        {
            public int Example { get; set; }
            public string Section { get; set; } = string.Empty;
            public string Html { get; set; } = string.Empty;
        }
    }
}
