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
