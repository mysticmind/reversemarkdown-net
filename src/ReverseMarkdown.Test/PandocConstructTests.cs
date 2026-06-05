using ReverseMarkdown;
using Xunit;

namespace ReverseMarkdown.Test
{
    // Phase E: Pandoc-specific constructs (heading attributes, fenced divs, bracketed spans).
    public class PandocConstructTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        private static string Render(string html, Config.MarkdownFlavor flavor)
        {
            var converter = new Converter(new Config());
            return Norm(converter.Render(converter.Parse(html), flavor));
        }

        [Fact]
        public void Pandoc_heading_attributes()
        {
            Assert.Equal("## Title {#sec .intro}",
                Render("<h2 id=\"sec\" class=\"intro\">Title</h2>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void Default_heading_drops_attributes()
        {
            Assert.Equal("## Title",
                Render("<h2 id=\"sec\" class=\"intro\">Title</h2>", Config.MarkdownFlavor.Default));
        }

        [Fact]
        public void Pandoc_fenced_div()
        {
            Assert.Equal("::: {.warning}\nBe careful.\n:::",
                Render("<div class=\"warning\"><p>Be careful.</p></div>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void Default_fenced_div_degrades_to_content()
        {
            Assert.Equal("Be careful.",
                Render("<div class=\"warning\"><p>Be careful.</p></div>", Config.MarkdownFlavor.Default));
        }

        [Fact]
        public void Pandoc_bracketed_span()
        {
            Assert.Equal("a [hi]{.hl} b",
                Render("<p>a <span class=\"hl\">hi</span> b</p>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void Default_bracketed_span_degrades_to_content()
        {
            Assert.Equal("a hi b",
                Render("<p>a <span class=\"hl\">hi</span> b</p>", Config.MarkdownFlavor.Default));
        }
    }
}
