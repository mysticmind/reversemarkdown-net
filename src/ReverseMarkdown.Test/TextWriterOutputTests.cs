using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ReverseMarkdown.Test
{
    // The TextWriter overloads stream the rendered buffer instead of building the result as a
    // string. They must produce exactly what the string path produces - these tests are the proof.
    public class TextWriterOutputTests
    {
        private static string ViaWriter(Converter converter, string html)
        {
            using var writer = new StringWriter();
            converter.Convert(html, writer);
            return writer.ToString();
        }

        public static TheoryData<string> Documents() => new()
        {
            "<p>simple</p>",
            "<h1>Title</h1><p>Body with <strong>bold</strong> and <a href=\"http://x.com\">a link</a>.</p>",
            "<ul><li>one</li><li>two<ul><li>nested</li></ul></li></ul>",
            "<table><tr><th>H1</th><th>H2</th></tr><tr><td>a</td><td>b</td></tr></table>",
            "<blockquote><p>quoted</p><p>again</p></blockquote>",
            "<pre><code class=\"language-cs\">var x = 1;\nvar y = 2;</code></pre>",
            "<p>trailing newlines</p>\n\n\n",
            "\n\n<p>leading newlines</p>",
            "<p>line<br>break</p>",
            "<p>unicode separators \u0085 and \u2028 and \u2029 here</p>",
            "<p>carriage\r\nreturn and bare\rcarriage</p>",
            "<p>form\ffeed</p>",
            "<img src=\"a.png\" alt=\"alt\">",
            "<dl><dt>term</dt><dd>definition</dd></dl>",
            "",
            "   ",
        };

        [Theory]
        [MemberData(nameof(Documents))]
        public void Streamed_output_matches_the_string_output(string html)
        {
            var converter = new Converter(new Config());

            Assert.Equal(converter.Convert(html), ViaWriter(converter, html));
        }

        [Theory]
        [InlineData(Config.MarkdownFlavor.Default)]
        [InlineData(Config.MarkdownFlavor.GitHub)]
        [InlineData(Config.MarkdownFlavor.CommonMark)]
        [InlineData(Config.MarkdownFlavor.Slack)]
        [InlineData(Config.MarkdownFlavor.Telegram)]
        [InlineData(Config.MarkdownFlavor.MultiMarkdown)]
        [InlineData(Config.MarkdownFlavor.Pandoc)]
        public void Streamed_output_matches_for_every_flavor(Config.MarkdownFlavor flavor)
        {
            const string html =
                "<h1>Title</h1><p>Body <strong>bold</strong> <em>italic</em> " +
                "<a href=\"http://x.com\">link</a></p><ul><li>one</li><li>two</li></ul>";
            var converter = new Converter(new Config { Flavor = flavor });

            Assert.Equal(converter.Convert(html), ViaWriter(converter, html));
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void Streamed_output_honours_the_configured_line_ending(string lineEnding)
        {
            var config = new Config { Formatting = { OutputLineEnding = lineEnding } };
            var converter = new Converter(config);
            const string html = "<p>one</p><p>two</p><ul><li>a</li><li>b</li></ul>";

            var streamed = ViaWriter(converter, html);

            Assert.Equal(converter.Convert(html), streamed);
            Assert.Contains($"one{lineEnding}", streamed);
        }

        [Fact]
        public void Render_overload_matches_the_string_render()
        {
            var converter = new Converter(new Config());
            var document = converter.Parse("<h1>T</h1><p>body</p>");

            using var writer = new StringWriter();
            converter.Render(document, writer);

            Assert.Equal(converter.Render(document), writer.ToString());
        }

        [Fact]
        public void Render_overload_matches_for_an_explicit_flavor()
        {
            var converter = new Converter(new Config());
            var document = converter.Parse("<p><strong>bold</strong></p>");

            using var writer = new StringWriter();
            converter.Render(document, Config.MarkdownFlavor.Pandoc, writer);

            Assert.Equal(converter.Render(document, Config.MarkdownFlavor.Pandoc), writer.ToString());
        }

        [Fact]
        public void Preprocessing_applies_on_the_streamed_path()
        {
            var config = new Config();
            config.Preprocess.Remove("nav").RemoveScripts();
            var converter = new Converter(config);
            const string html = "<nav>menu</nav><p>body</p><script>x()</script>";

            Assert.Equal(converter.Convert(html), ViaWriter(converter, html));
            Assert.Equal("body", ViaWriter(converter, html).Trim());
        }

        [Fact]
        public void A_null_writer_is_rejected()
        {
            var converter = new Converter(new Config());

            Assert.Throws<ArgumentNullException>(() => converter.Convert("<p>x</p>", null!));
            Assert.Throws<ArgumentNullException>(() => converter.Render(converter.Parse("<p>x</p>"), null!));
        }

        [Fact]
        public void A_document_larger_than_the_streaming_block_matches()
        {
            // The writer copies in 8 KB blocks, so a line ending landing on a block boundary is the
            // interesting case: build a document comfortably larger than one block.
            var html = string.Concat(Enumerable.Range(0, 2000)
                .Select(i => $"<p>Paragraph {i} with enough text to push past the block size.</p>"));
            var converter = new Converter(new Config());

            Assert.Equal(converter.Convert(html), ViaWriter(converter, html));
        }
    }
}
