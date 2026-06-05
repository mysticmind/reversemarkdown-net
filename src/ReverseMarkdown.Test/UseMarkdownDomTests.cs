using ReverseMarkdown;
using Xunit;

namespace ReverseMarkdown.Test
{
    // The v6 opt-in switch: Config.UseMarkdownDom routes Convert through the Markdown DOM path.
    public class UseMarkdownDomTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        [Fact]
        public void Convert_uses_v6_path_when_opted_in()
        {
            var v6 = new Converter(new Config { UseMarkdownDom = true });
            var doc = "<h1>Hi</h1><p>Hello <strong>world</strong></p>";
            Assert.Equal("# Hi\n\nHello **world**", Norm(v6.Convert(doc)));
        }

        [Fact]
        public void Convert_uses_v6_flavor_when_opted_in()
        {
            var slack = new Converter(new Config { UseMarkdownDom = true, Flavor = Config.MarkdownFlavor.Slack });
            Assert.Equal("*bold*", Norm(slack.Convert("<p><strong>bold</strong></p>")));
        }

        [Fact]
        public void Default_convert_still_uses_v5_path()
        {
            // v5 default renders an HR as "* * *"; v6 renders "---". Default config must stay v5.
            var v5 = new Converter(new Config());
            Assert.Contains("* * *", v5.Convert("<hr>"));
        }
    }
}
