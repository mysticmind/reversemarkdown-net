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
}
