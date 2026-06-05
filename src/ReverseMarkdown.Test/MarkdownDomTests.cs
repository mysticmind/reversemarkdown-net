using System.Linq;
using ReverseMarkdown;
using ReverseMarkdown.Dom;
using Xunit;

namespace ReverseMarkdown.Test
{
    // Phase A scaffolding tests for the v6 Markdown DOM path. See docs/v6/.
    public class MarkdownDomTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        [Fact]
        public void Parse_then_render_roundtrips_through_commonmark()
        {
            var converter = new Converter(new Config());

            var doc = converter.Parse("<h1>Hi</h1><p>Hello <strong>world</strong> and <em>you</em></p>");
            var md = converter.Render(doc);

            Assert.Equal("# Hi\n\nHello **world** and *you*", Norm(md));
        }

        [Fact]
        public void Parse_builds_a_navigable_tree()
        {
            var converter = new Converter(new Config());

            var doc = converter.Parse("<p>Hello <strong>world</strong></p>");

            Assert.Single(doc.Children);
            var paragraph = Assert.IsType<MdParagraph>(doc.Children[0]);
            Assert.Collection(paragraph.Children,
                inline => Assert.Equal("Hello ", Assert.IsType<MdText>(inline).Value),
                inline => Assert.IsType<MdStrong>(inline));
        }

        [Fact]
        public void Dom_is_mutable_remove_drops_node_from_output()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse("<p>Hello <strong>world</strong></p>");

            var strong = doc.Descendants().OfType<MdStrong>().Single();
            strong.Remove();

            Assert.Equal("Hello", Norm(converter.Render(doc)));
            Assert.Null(strong.Parent);
        }

        [Fact]
        public void Dom_is_mutable_replacewith_swaps_node()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse("<p>Hello <strong>world</strong></p>");

            var strong = doc.Descendants().OfType<MdStrong>().Single();
            strong.ReplaceWith(new MdEmphasis { Children = { new MdText("there") } });

            Assert.Equal("Hello *there*", Norm(converter.Render(doc)));
        }

        [Theory]
        [InlineData("<p>see <a href=\"https://x.io\">site</a></p>", "see [site](https://x.io)")]
        [InlineData("<p>see <a href=\"https://x.io\" title=\"t\">site</a></p>", "see [site](https://x.io \"t\")")]
        [InlineData("<p>a <img src=\"i.png\" alt=\"pic\"> b</p>", "a ![pic](i.png) b")]
        [InlineData("<p>run <code>dotnet build</code></p>", "run `dotnet build`")]
        [InlineData("<p>x<br>y</p>", "x  \ny")]
        [InlineData("<p>a <s>b</s> c</p>", "a ~~b~~ c")]
        [InlineData("<p>a <del>b</del> c</p>", "a ~~b~~ c")]
        public void Inline_nodes_render_through_commonmark(string html, string expected)
        {
            var converter = new Converter(new Config());
            Assert.Equal(expected, Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void Thematic_break_renders()
        {
            var converter = new Converter(new Config());
            Assert.Equal("a\n\n---\n\nb", Norm(converter.Render(converter.Parse("<p>a</p><hr><p>b</p>"))));
        }

        [Fact]
        public void Blockquote_prefixes_each_line()
        {
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse("<blockquote><p>one</p><p>two</p></blockquote>"));
            Assert.Equal("> one\n>\n> two", Norm(md));
        }

        [Fact]
        public void Inline_code_widens_fence_when_content_has_backtick()
        {
            var converter = new Converter(new Config());
            // HtmlAgilityPack keeps the backtick literal inside <code>
            var md = converter.Render(converter.Parse("<p><code>a`b</code></p>"));
            Assert.Equal("``a`b``", Norm(md));
        }

        [Fact]
        public void Child_collections_maintain_parent_backpointer()
        {
            var paragraph = new MdParagraph();
            var text = new MdText("x");

            paragraph.Children.Add(text);
            Assert.Same(paragraph, text.Parent);

            paragraph.Children.Remove(text);
            Assert.Null(text.Parent);
        }
    }
}
