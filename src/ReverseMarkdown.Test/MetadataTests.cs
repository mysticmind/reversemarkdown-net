using ReverseMarkdown;
using Xunit;

namespace ReverseMarkdown.Test
{
    // Phase E: document metadata (MMD key:value pairs / Pandoc YAML frontmatter).
    public class MetadataTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        private const string Html =
            "<html><head><title>My Title</title><meta name=\"author\" content=\"Jane\"></head>" +
            "<body><p>Body.</p></body></html>";

        [Fact]
        public void MultiMarkdown_emits_key_value_metadata()
        {
            var converter = new Converter(new Config());
            var md = Norm(converter.Render(converter.Parse(Html), Config.MarkdownFlavor.MultiMarkdown));
            Assert.Equal("title: My Title\nauthor: Jane\n\nBody.", md);
        }

        [Fact]
        public void Pandoc_emits_yaml_frontmatter()
        {
            var converter = new Converter(new Config());
            var md = Norm(converter.Render(converter.Parse(Html), Config.MarkdownFlavor.Pandoc));
            Assert.Equal("---\ntitle: My Title\nauthor: Jane\n---\n\nBody.", md);
        }

        [Fact]
        public void Default_flavor_drops_metadata()
        {
            var converter = new Converter(new Config());
            var md = Norm(converter.Render(converter.Parse(Html), Config.MarkdownFlavor.Default));
            Assert.Equal("Body.", md);
        }

        [Theory]
        [InlineData(Config.MarkdownFlavor.MultiMarkdown, "see [#doe2020] now")]
        [InlineData(Config.MarkdownFlavor.Pandoc, "see [@doe2020] now")]
        [InlineData(Config.MarkdownFlavor.Default, "see *Doe 2020* now")]
        public void Citation_renders_per_flavor(Config.MarkdownFlavor flavor, string expected)
        {
            var converter = new Converter(new Config());
            var html = "<p>see <cite data-cite=\"doe2020\">Doe 2020</cite> now</p>";
            Assert.Equal(expected, Norm(converter.Render(converter.Parse(html), flavor)));
        }
    }
}
