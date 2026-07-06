using System.Linq;
using ReverseMarkdown;
using ReverseMarkdown.Dom;
using Xunit;

namespace ReverseMarkdown.Test
{
    // Issue #79: structured, filterable output via the v6 Markdown DOM path.
    public class Issue79ApiTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        [Fact]
        public void HtmlExcludeSelectors_drops_matching_elements_before_conversion()
        {
            var config = new Config();
            config.HtmlExcludeSelectors.Add("div.ad");
            var converter = new Converter(config);

            var html = "<p>keep</p><div class=\"ad\"><p>spam</p></div><p>also keep</p>";
            Assert.Equal("keep\n\nalso keep", Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void HtmlExcludeSelectors_supports_multiple_and_grouped_selectors()
        {
            var config = new Config();
            config.HtmlExcludeSelectors.Add("nav, aside.related");
            var converter = new Converter(config);

            var html = "<nav><p>menu</p></nav><p>body</p><aside class=\"related\"><p>links</p></aside>";
            Assert.Equal("body", Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void HtmlElementFilters_predicate_drops_elements()
        {
            var config = new Config();
            config.HtmlElementFilters.Add(e => e.GetAttribute("data-role") == "promo");
            var converter = new Converter(config);

            var html = "<p>real</p><p data-role=\"promo\">buy now</p>";
            Assert.Equal("real", Norm(converter.Render(converter.Parse(html))));
        }

        [Fact]
        public void MarkdownDom_is_queryable_for_picking()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse("<h1>T</h1><p>a</p><h2>U</h2><p>b</p>");

            var headings = doc.Descendants().OfType<MdHeading>().ToList();
            Assert.Equal(2, headings.Count);
            Assert.Equal(new[] { 1, 2 }, headings.Select(h => h.Level));
        }

        [Fact]
        public void RemoveWhere_filters_markdown_side()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse("<p>text</p><p>pic <img src=\"x.png\" alt=\"a\"> here</p>");

            var removed = doc.RemoveWhere(n => n is MdImage);

            Assert.Equal(1, removed);
            Assert.DoesNotContain("![", converter.Render(doc));
        }

        [Fact]
        public void Same_tree_renders_to_multiple_flavors()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse("<p><strong>bold</strong></p>");

            var cm = converter.Render(doc, Config.MarkdownFlavor.CommonMark);
            var gh = converter.Render(doc, Config.MarkdownFlavor.GitHub);

            Assert.Equal("**bold**", Norm(cm));
            Assert.Equal("**bold**", Norm(gh));
        }
    }
}
