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
        public void Pandoc_preserves_inline_code_edge_spaces_with_raw_code()
        {
            Assert.Equal("<code> a</code>",
                Render("<p><code> a</code></p>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void Pandoc_preserves_tabs_in_code_blocks_with_raw_pre()
        {
            Assert.Equal("<pre><code>foo\tbaz</code></pre>",
                Render("<pre><code>foo\tbaz\n</code></pre>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void Pandoc_pre_language_attribute_is_not_syntax_highlight_class()
        {
            Assert.Equal("```{language=\"haskell\"}\nmain = pure ()\n```",
                Render("<pre language=\"haskell\"><code>main = pure ()\n</code></pre>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void Pandoc_preserves_multiline_link_titles_as_raw_anchor()
        {
            Assert.Equal("<a href=\"/url\" title=\"\ntitle\nline1\n\">foo</a>",
                Render("<p><a href=\"/url\" title=\"\ntitle\nline1\n\">foo</a></p>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void Pandoc_escapes_literal_at_signs_to_avoid_citations()
        {
            Assert.Equal("&lt;foo+\\@bar.example.com&gt;",
                Render("<p>&lt;foo+@bar.example.com&gt;</p>", Config.MarkdownFlavor.Pandoc));
        }

        [Fact]
        public void Default_bracketed_span_degrades_to_content()
        {
            Assert.Equal("a hi b",
                Render("<p>a <span class=\"hl\">hi</span> b</p>", Config.MarkdownFlavor.Default));
        }
    }
}
