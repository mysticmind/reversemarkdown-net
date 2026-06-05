using AngleSharp.Dom;
using ReverseMarkdown;
using ReverseMarkdown.Dom;
using ReverseMarkdown.Readers;
using Xunit;

namespace ReverseMarkdown.Test
{
    // A custom external reader, auto-discovered via [MarkdownReader] from this test assembly.
    [MarkdownReader("mark")]
    public sealed class HighlightReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            ctx.Emit(new MdRawInline("==") { SourceTag = element.LocalName });
            ctx.ReadChildren(element);
            ctx.Emit(new MdRawInline("==") { SourceTag = element.LocalName });
        }
    }

    public class CustomReaderTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        [Fact]
        public void Custom_reader_is_discovered_from_additional_assembly()
        {
            var converter = new Converter(new Config(), typeof(HighlightReader).Assembly);
            var md = converter.Render(converter.Parse("<p>a <mark>b</mark> c</p>"));
            Assert.Equal("a ==b== c", Norm(md));
        }

        [Fact]
        public void Without_additional_assembly_the_custom_tag_is_unknown()
        {
            // Default UnknownTags = PassThrough -> raw HTML kept.
            var converter = new Converter(new Config());
            var md = converter.Render(converter.Parse("<p>a <mark>b</mark> c</p>"));
            Assert.Equal("a <mark>b</mark> c", Norm(md));
        }
    }
}
