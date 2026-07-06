using ReverseMarkdown;
using Xunit;

namespace ReverseMarkdown.Test
{
    // Phase E: math (\(..\) vs $..$) and abbreviations.
    public class MathAndAbbrTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        private static string Render(string html, Config.MarkdownFlavor flavor)
        {
            var converter = new Converter(new Config());
            return Norm(converter.Render(converter.Parse(html), flavor));
        }

        [Theory]
        [InlineData(Config.MarkdownFlavor.MultiMarkdown, "x \\(a^2\\) y")]
        [InlineData(Config.MarkdownFlavor.Pandoc, "x $a^2$ y")]
        public void Inline_math_per_flavor(Config.MarkdownFlavor flavor, string expected)
        {
            // Pandoc-style HTML where the TeX is wrapped in \(..\).
            Assert.Equal(expected, Render("<p>x <span class=\"math inline\">\\(a^2\\)</span> y</p>", flavor));
        }

        [Theory]
        [InlineData(Config.MarkdownFlavor.MultiMarkdown, "\\[a^2\\]")]
        [InlineData(Config.MarkdownFlavor.Pandoc, "$$a^2$$")]
        public void Display_math_per_flavor(Config.MarkdownFlavor flavor, string expected)
        {
            Assert.Equal(expected, Render("<p><span class=\"math display\">\\[a^2\\]</span></p>", flavor));
        }

        [Fact]
        public void Math_in_code_element_is_detected()
        {
            Assert.Equal("$a$", Render("<p><code class=\"math inline\">a</code></p>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void MultiMarkdown_appends_abbreviation_definitions()
        {
            var html = "<p>The <abbr title=\"HyperText Markup Language\">HTML</abbr> spec.</p>";
            Assert.Equal("The HTML spec.\n\n*[HTML]: HyperText Markup Language",
                Render(html, Config.MarkdownFlavor.MultiMarkdown));
        }

        [Fact]
        public void Default_flavor_keeps_abbr_text_only()
        {
            var html = "<p>The <abbr title=\"HyperText Markup Language\">HTML</abbr> spec.</p>";
            Assert.Equal("The HTML spec.", Render(html, Config.MarkdownFlavor.Default));
        }
    }
}
