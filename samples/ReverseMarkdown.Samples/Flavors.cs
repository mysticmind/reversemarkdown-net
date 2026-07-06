using ReverseMarkdown;
using MarkdownFlavor = ReverseMarkdown.Config.MarkdownFlavor;

namespace Samples;

public static class Flavors
{
    public static void Select()
    {
        #region sample_flavor_select
        var config = new Config { Flavor = MarkdownFlavor.CommonMark };
        var converter = new Converter(config);
        #endregion
    }

    public static void GitHubWriter()
    {
        #region sample_github_flavor
        var config = new Config { Flavor = MarkdownFlavor.GitHub };
        #endregion
        _ = config;
    }

    public static void GithubFlavored()
    {
        #region sample_github_flavored
        var config = new Config { GithubFlavored = true };
        #endregion
        _ = config;
    }

    public static void CommonMark()
    {
        #region sample_commonmark
        var config = new Config { Flavor = MarkdownFlavor.CommonMark };
        var converter = new Converter(config);
        #endregion
    }

    public static void CommonMarkOptions()
    {
        #region sample_commonmark_options
        var config = new Config
        {
            Flavor = MarkdownFlavor.CommonMark,
            CommonMarkIntrawordEmphasisSpacing = true,
            CommonMarkUseHtmlInlineTags = false,
        };
        #endregion
        _ = config;
    }

    public static void Slack()
    {
        #region sample_slack
        var config = new Config { Flavor = MarkdownFlavor.Slack };
        var converter = new Converter(config);
        #endregion
    }

    public static void Telegram()
    {
        #region sample_telegram
        var converter = new Converter(new Config { Flavor = MarkdownFlavor.Telegram });

        var html = "This is <strong>bold</strong>, <em>italic</em>, <del>strikethrough</del> " +
                   "and <a href=\"https://example.com/path_(one)?q=1)2\">a_b[c]</a>";
        var result = converter.Convert(html);
        // This is *bold*, _italic_, ~strikethrough~ and [a\_b\[c\]](https://example.com/path_(one\)?q=1\)2)
        #endregion
    }

    public static void MultiMarkdown()
    {
        #region sample_mmd
        var config = new Config { Flavor = MarkdownFlavor.MultiMarkdown };
        var converter = new Converter(config);
        #endregion
    }

    public static void Pandoc()
    {
        #region sample_pandoc
        var config = new Config { Flavor = MarkdownFlavor.Pandoc };
        var converter = new Converter(config);
        #endregion
    }
}
