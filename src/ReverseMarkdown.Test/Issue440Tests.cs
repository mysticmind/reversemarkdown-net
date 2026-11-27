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

        // Follow-ups from #440: leftovers the clean flavors used to write as links to nothing or raw HTML.
        [Theory]
        [InlineData("<p><a href=\"https://example.org/x\"><div>One</div><div>Two</div></a> after</p>",
            "[One](https://example.org/x)\n\n[Two](https://example.org/x)\n\nafter")]
        [InlineData("<p>x <a href=\"https://example.org/x\"></a> y</p>", "x y")]
        [InlineData("<p>x <a href=\"https://example.org/x\"> </a> y</p>", "x y")]
        [InlineData("<p>Top <a name=\"top\"></a> here <a id=\"x\">named</a> end</p>", "Top here named end")]
        [InlineData("<a href=\"https://example.org/x\"><img src=\"https://example.org/i.png\" alt=\"\"></a>",
            "[![](https://example.org/i.png)](https://example.org/x)")]
        [InlineData("<p>Posted <time datetime=\"2026-11-26\">today</time>, <data value=\"1\">one</data>, <bdi>b</bdi>, <bdo dir=\"rtl\">c</bdo></p>",
            "Posted today, one, b, c")]
        [InlineData("<a href=\"https://example.org/n\"><h3>Dimorphos</h3><time datetime=\"2026-11-26\">vor 22 Minuten</time><div>heise online</div></a>",
            "[Dimorphos vor 22 Minuten heise online](https://example.org/n)")]
        public void Clean_flavor_drops_empty_links_and_unwraps_semantic_only_tags(string html, string expected)
        {
            Assert.Equal(expected, Convert(html));
        }

        [Theory]
        [InlineData("<p>x <a href=\"https://example.org/x\"></a> <a name=\"top\"></a> y</p>")]
        [InlineData("<p>Posted <time datetime=\"2026-11-26\">today</time></p>")]
        public void CommonMark_keeps_raw_html(string html)
        {
            var config = new Config { Flavor = Config.MarkdownFlavor.CommonMark };
            var markdown = new Converter(config).Convert(html).Replace("\r\n", "\n").Trim();
            Assert.Equal(html.Substring(3, html.Length - 7), markdown);
        }

        [Fact]
        public void Explicit_pass_through_still_wins_for_semantic_only_tags()
        {
            var config = new Config();
            config.Tags.PassThrough.Add("time");
            var markdown = new Converter(config).Convert("<p>Posted <time>today</time></p>").Trim();
            Assert.Equal("Posted <time>today</time>", markdown);
        }
    }
}
