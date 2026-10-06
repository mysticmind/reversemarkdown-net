using System.IO;
using ReverseMarkdown;
using MarkdownFlavor = ReverseMarkdown.Config.MarkdownFlavor;

namespace Samples;

// Snippets referenced from the docs. Regions are extracted by
// @radarleaf/markdown-it-region-snippets; using directives above stay out of the snippets.
public static class Basics
{
    public static void BasicUsage()
    {
        #region sample_basic_usage
        var converter = new Converter();

        string html = "This a sample <strong>paragraph</strong> from " +
                      "<a href=\"http://test.com\">my site</a>";

        string result = converter.Convert(html);
        // This a sample **paragraph** from [my site](http://test.com)
        #endregion
    }

    public static void WithConfig()
    {
        #region sample_with_config
        var config = new Config
        {
            // generate GitHub flavoured markdown (br, pre -> fenced code, task lists)
            GithubFlavored = true,
            // include unknown tags completely in the result (the default)
            Tags = { Unknown = Config.UnknownTagsOption.PassThrough },
            // ignore all comments
            Formatting = { RemoveComments = true },
            // collapse a link to plain text when text and href match
            Links = { SmartHref = true },
        };

        var converter = new Converter(config);
        #endregion
    }

    public static void StreamToWriter(string html)
    {
        #region sample_stream_to_writer
        var converter = new Converter();

        // Renders straight into the writer: the Markdown is never held as one string.
        using var file = new StreamWriter("page.md");
        converter.Convert(html, file);
        #endregion
    }

    public static void RenderToWriter(string html, TextWriter output)
    {
        #region sample_render_to_writer
        var converter = new Converter();

        var document = converter.Parse(html);
        converter.Render(document, output);
        #endregion
    }
}
