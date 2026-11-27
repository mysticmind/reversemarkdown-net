using Xunit;

namespace ReverseMarkdown.Test
{
    /// <summary>Issue #440: blocks flattened into an inline element must not run together.</summary>
    public class Issue440Tests
    {
        private static string Convert(string html) =>
            new Converter().Convert(html).Replace("\r\n", "\n").Trim();

        [Theory]
        [InlineData("<a href=\"https://example.org/news/2\"><h3>Second headline</h3><p>A summary of it.</p></a>",
            "[Second headline A summary of it.](https://example.org/news/2)")]
        [InlineData("<a href=\"https://example.org/7\"><div>One</div><div>Two</div></a>",
            "[One Two](https://example.org/7)")]
        [InlineData("<a href=\"https://example.org/list\"><ul><li>One</li><li>Two</li></ul></a>",
            "[One Two](https://example.org/list)")]
        [InlineData("<a href=\"https://example.org/news/3\">\n<h3>Title</h3>\n<p>Summary</p>\n</a>",
            "[Title Summary](https://example.org/news/3)")]
        [InlineData("<a href=\"https://example.org/x\">Lead<div>One</div>Tail</a>",
            "[Lead One Tail](https://example.org/x)")]
        [InlineData("<a href=\"https://example.org/x\"><div>One</div></a>",
            "[One](https://example.org/x)")]
        [InlineData("<del><p>One</p><p>Two</p></del>", "~~One Two~~")]
        [InlineData("<a href=\"https://example.org/x\"><div><strong>One</strong></div><div><em>Two</em></div></a>",
            "[**One** *Two*](https://example.org/x)")]
        [InlineData("<a href=\"https://example.org/x\"><strong><div>One</div><div>Two</div></strong></a>",
            "[**One Two**](https://example.org/x)")]
        [InlineData("<a href=\"https://example.org/x\"><div>One </div><div>Two</div></a>",
            "[One Two](https://example.org/x)")]
        [InlineData("<a href=\"https://example.org/x\"><div>One</div><div></div><div>Two</div></a>",
            "[One Two](https://example.org/x)")]
        [InlineData("<a href=\"https://example.org/n\"><h3>Dimorphos</h3><span>vor 22 Minuten</span><div>heise online</div></a>",
            "[Dimorphos vor 22 Minuten heise online](https://example.org/n)")]
        public void Adjacent_blocks_inside_inline_are_separated(string html, string expected)
        {
            Assert.Equal(expected, Convert(html));
        }
    }
}
