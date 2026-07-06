using System.Linq;
using ReverseMarkdown;
using ReverseMarkdown.Dom;
using Xunit;

namespace ReverseMarkdown.Test
{
    // Phase E: footnotes (shared MMD/Pandoc/GFM feature).
    public class FootnoteTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        // Pandoc-style footnote HTML.
        private const string Html =
            "<p>Text<a href=\"#fn1\" class=\"footnote-ref\" id=\"fnref1\"><sup>1</sup></a>.</p>" +
            "<section class=\"footnotes\"><ol>" +
            "<li id=\"fn1\"><p>The note. <a href=\"#fnref1\" class=\"footnote-back\">↩</a></p></li>" +
            "</ol></section>";

        [Fact]
        public void Footnote_reference_and_definition_render()
        {
            var converter = new Converter(new Config());
            var md = Norm(converter.Render(converter.Parse(Html)));
            Assert.Equal("Text[^1].\n\n[^1]: The note.", md);
        }

        [Fact]
        public void Footnote_reference_is_a_queryable_node()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse(Html);

            Assert.Single(doc.Descendants().OfType<MdFootnoteReference>());
            Assert.Single(doc.Meta.Footnotes);
            Assert.Equal("1", doc.Meta.Footnotes[0].Id);
        }

        [Fact]
        public void Back_reference_arrow_is_suppressed()
        {
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse(Html));
            Assert.DoesNotContain("↩", md);
        }

        [Fact]
        public void Footnotes_render_across_flavors()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse(Html);
            foreach (var flavor in new[] { Config.MarkdownFlavor.MultiMarkdown, Config.MarkdownFlavor.Pandoc, Config.MarkdownFlavor.GitHub })
            {
                Assert.Contains("[^1]", converter.Render(doc, flavor));
            }
        }
    }
}
