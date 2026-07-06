using ReverseMarkdown;
using Xunit;

namespace ReverseMarkdown.Test
{
    public class AngleSharpMarkdownDomTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        [Fact]
        public void Convert_uses_v6_path_by_default()
        {
            var v6 = new Converter(new Config());
            var doc = "<h1>Hi</h1><p>Hello <strong>world</strong></p>";
            Assert.Equal("# Hi\n\nHello **world**", Norm(v6.Convert(doc)));
        }

        [Fact]
        public void Convert_uses_v6_flavor_by_default()
        {
            var slack = new Converter(new Config { Flavor = Config.MarkdownFlavor.Slack });
            Assert.Equal("*bold*", Norm(slack.Convert("<p><strong>bold</strong></p>")));
        }

    }
}
