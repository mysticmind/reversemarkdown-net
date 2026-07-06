using ReverseMarkdown;
using Xunit;

namespace ReverseMarkdown.Test
{
    // v6 per-flavor writers: one parsed tree, many target flavors.
    public class FlavorWriterTests
    {
        private static string Norm(string s) => s.Replace("\r\n", "\n").Trim();

        private static string Render(string html, Config.MarkdownFlavor flavor)
        {
            var converter = new Converter(new Config());
            return Norm(converter.Render(converter.Parse(html), flavor));
        }

        [Theory]
        [InlineData("<p><strong>b</strong></p>", "*b*")]
        [InlineData("<p><em>i</em></p>", "_i_")]
        [InlineData("<p><s>x</s></p>", "~x~")]
        [InlineData("<ul><li>one</li></ul>", "• one")]
        [InlineData("<h1>Title</h1>", "*Title*")]
        [InlineData("<p><a href=\"https://x.io\">site</a></p>", "<https://x.io|site>")]
        public void Slack_flavor(string html, string expected)
        {
            Assert.Equal(expected, Render(html, Config.MarkdownFlavor.Slack));
        }

        [Fact]
        public void Slack_throws_on_unsupported_table()
        {
            var converter = new Converter(new Config());
            var doc = converter.Parse("<table><tr><th>A</th></tr><tr><td>1</td></tr></table>");
            Assert.Throws<SlackUnsupportedTagException>(() => converter.Render(doc, Config.MarkdownFlavor.Slack));
        }

        [Theory]
        [InlineData("<p><strong>b</strong></p>", "*b*")]
        [InlineData("<p>a.b!</p>", "a\\.b\\!")]              // MarkdownV2 escaping
        [InlineData("<hr>", "\\-\\-\\-")]
        public void Telegram_flavor(string html, string expected)
        {
            Assert.Equal(expected, Render(html, Config.MarkdownFlavor.Telegram));
        }

        [Theory]
        [InlineData(Config.MarkdownFlavor.MultiMarkdown)]
        [InlineData(Config.MarkdownFlavor.Pandoc)]
        public void Subscript_is_native_in_mmd_and_pandoc(Config.MarkdownFlavor flavor)
        {
            Assert.Equal("H~2~O", Render("<p>H<sub>2</sub>O</p>", flavor));
        }

        [Fact]
        public void Default_subscript_degrades_to_html()
        {
            Assert.Equal("H<sub>2</sub>O", Render("<p>H<sub>2</sub>O</p>", Config.MarkdownFlavor.Default));
        }

        [Fact]
        public void Github_matches_base_for_table()
        {
            Assert.Equal("| A |\n| --- |\n| 1 |",
                Render("<table><tr><th>A</th></tr><tr><td>1</td></tr></table>", Config.MarkdownFlavor.GitHub));
        }
    }
}
