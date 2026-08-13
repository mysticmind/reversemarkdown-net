using System.Text.RegularExpressions;
using ReverseMarkdown;
using ReverseMarkdown.Preprocessing;

namespace Samples;

/// <summary>
/// One worked example per preprocessing helper. Each region shows the input HTML, the step, and the
/// result as a trailing comment; <c>PreprocessingStepSampleTests</c> in the test project asserts
/// every one of those results, so a comment cannot drift from what the code actually produces.
/// </summary>
public static class PreprocessingSteps
{
    // ---- Removal ----

    public static string Remove()
    {
        #region sample_step_remove
        var config = new Config();
        config.Preprocess.Remove("nav, footer, .advertisement");

        var markdown = new Converter(config).Convert(
            "<nav>menu</nav><p>Body text.</p><footer>fine print</footer>");
        // Body text.
        #endregion
        return markdown;
    }

    public static string RemoveWhere()
    {
        #region sample_step_removewhere
        var config = new Config();
        config.Preprocess.RemoveWhere(e => e.GetAttribute("data-role") == "promo");

        var markdown = new Converter(config).Convert(
            "<p>Real content.</p><p data-role=\"promo\">Buy now!</p>");
        // Real content.
        #endregion
        return markdown;
    }

    public static string KeepOnly()
    {
        #region sample_step_keeponly
        var config = new Config();
        config.Preprocess.KeepOnly("article");

        var markdown = new Converter(config).Convert(
            "<header>site nav</header><article><h1>Title</h1><p>Body.</p></article><footer>end</footer>");
        // # Title
        //
        // Body.
        #endregion
        return markdown;
    }

    public static string RemoveComments()
    {
        #region sample_step_removecomments
        var config = new Config();
        config.Preprocess.RemoveComments();

        var markdown = new Converter(config).Convert("<p>Visible<!-- editor note --> text.</p>");
        // Visible text.
        #endregion
        return markdown;
    }

    public static string RemoveEmptyElements()
    {
        #region sample_step_removeempty
        var config = new Config();
        config.Preprocess.RemoveEmptyElements();

        var markdown = new Converter(config).Convert(
            "<p>Kept.</p><p></p><div><span>  </span></div><p>Also kept.</p>");
        // Kept.
        //
        // Also kept.
        #endregion
        return markdown;
    }

    // ---- Element and content transformation ----

    public static string Rename()
    {
        #region sample_step_rename
        var config = new Config();
        config.Preprocess.Rename("h1", "h2");   // demote headings a level

        var markdown = new Converter(config).Convert("<h1>Title</h1><p>Body.</p>");
        // ## Title
        //
        // Body.
        #endregion
        return markdown;
    }

    public static string Unwrap()
    {
        #region sample_step_unwrap
        var config = new Config();
        config.Preprocess.Unwrap("span");

        var markdown = new Converter(config).Convert("<p>a <span>b <em>c</em></span> d</p>");
        // a b *c* d
        #endregion
        return markdown;
    }

    public static string Wrap()
    {
        #region sample_step_wrap
        var config = new Config();
        config.Preprocess.Wrap("p.note", "blockquote");

        var markdown = new Converter(config).Convert("<p class=\"note\">Remember this.</p>");
        // > Remember this.
        #endregion
        return markdown;
    }

    public static string ReplaceWith()
    {
        #region sample_step_replacewith
        var config = new Config();
        config.Preprocess.ReplaceWith("div.callout", "<blockquote>Note</blockquote>");

        var markdown = new Converter(config).Convert(
            "<p>Before.</p><div class=\"callout\"><span>markup we do not want</span></div>");
        // Before.
        //
        // > Note
        #endregion
        return markdown;
    }

    public static string ReplaceWithText()
    {
        #region sample_step_replacewithtext
        var config = new Config();
        config.Preprocess.ReplaceWithText("img.emoji", ":smile:");

        var markdown = new Converter(config).Convert(
            "<p>Hi <img class=\"emoji\" src=\"smile.png\" alt=\"smile\"> there.</p>");
        // Hi :smile: there.
        #endregion
        return markdown;
    }

    public static string Transform()
    {
        #region sample_step_transform
        var config = new Config();
        config.Preprocess.Transform(
            "a[href^='/']",
            a => a.SetAttribute("href", "https://example.com" + a.GetAttribute("href")));

        var markdown = new Converter(config).Convert("<p><a href=\"/docs\">the docs</a></p>");
        // [the docs](https://example.com/docs)
        #endregion
        return markdown;
    }

    public static string ReplaceTextOrdinal()
    {
        #region sample_step_replacetext
        var config = new Config();
        config.Preprocess.ReplaceText("{{name}}", "Jane");

        var markdown = new Converter(config).Convert("<p>Hello {{name}}, welcome back.</p>");
        // Hello Jane, welcome back.
        #endregion
        return markdown;
    }

    public static string ReplaceTextRegex()
    {
        #region sample_step_replacetext_regex
        var config = new Config();
        config.Preprocess.ReplaceText(new Regex(@"\d{3}-\d{4}"), "[redacted]");

        var markdown = new Converter(config).Convert("<p>Call 555-1234 today.</p>");
        // Call [redacted] today.
        #endregion
        return markdown;
    }

    // ---- Attributes, styles and scripts ----

    public static string RemoveAttributes()
    {
        #region sample_step_removeattributes
        var config = new Config();
        config.Preprocess.RemoveAttributes("*", "data-*", "id");   // "data-*" is a prefix match

        // Preprocess returns the transformed HTML, which is where attributes are visible.
        var html = new Converter(config).Preprocess(
            "<p id=\"lead\" data-track=\"1\" class=\"intro\">Text.</p>");
        // <p class="intro">Text.</p>
        #endregion
        return html;
    }

    public static string RemoveInlineStyles()
    {
        #region sample_step_removeinlinestyles
        var config = new Config();
        config.Preprocess.RemoveInlineStyles();

        var html = new Converter(config).Preprocess("<p style=\"color:red\" id=\"a\">Text.</p>");
        // <p id="a">Text.</p>
        #endregion
        return html;
    }

    public static string RemoveStyleSheets()
    {
        #region sample_step_removestylesheets
        var config = new Config();
        config.Preprocess.RemoveStyleSheets();

        // The HTML5 parser hoists a leading <style> into <head>; steps run against the whole
        // document, so it is still reachable.
        var markdown = new Converter(config).Convert("<style>p { color: red }</style><p>Body.</p>");
        // Body.
        #endregion
        return markdown;
    }

    public static string RemoveStyles()
    {
        #region sample_step_removestyles
        var config = new Config();
        config.Preprocess.RemoveStyles();   // inline styles + <style> + stylesheet <link>

        var html = new Converter(config).Preprocess(
            "<style>p { color: red }</style><p style=\"color:blue\">Text.</p>");
        // <p>Text.</p>
        #endregion
        return html;
    }

    public static string RemoveClasses()
    {
        #region sample_step_removeclasses
        var config = new Config();
        // Spare <pre>/<code>: fenced code block languages are detected from their classes.
        config.Preprocess.RemoveClasses(":not(pre):not(code)");

        var html = new Converter(config).Preprocess("<p class=\"lead\">Text.</p>");
        // <p>Text.</p>
        #endregion
        return html;
    }

    public static string RemoveScripts()
    {
        #region sample_step_removescripts
        var config = new Config();
        config.Preprocess.RemoveScripts();   // <script>, <noscript> and on* handlers

        var markdown = new Converter(config).Convert(
            "<script>track()</script><p onclick=\"go()\">Body.</p><noscript>Enable JS</noscript>");
        // Body.
        #endregion
        return markdown;
    }

    // ---- Inline formatting ----

    public static string RemoveHidden()
    {
        #region sample_step_removehidden
        var config = new Config();
        config.Preprocess.RemoveHidden();

        var markdown = new Converter(config).Convert(
            "<p style=\"display:none\">Hidden preheader.</p><p>Visible.</p><p hidden>Also hidden.</p>");
        // Visible.
        #endregion
        return markdown;
    }

    public static string ConvertInlineStylesToTags()
    {
        #region sample_step_convertinlinestyles
        var config = new Config();
        config.Preprocess
            .ConvertInlineStylesToTags()
            .Unwrap("span");

        var markdown = new Converter(config).Convert(
            "<p><span style=\"font-weight:700\">bold</span> and " +
            "<span style=\"font-style:italic\">italic</span></p>");
        // **bold** and *italic*
        #endregion
        return markdown;
    }

    public static string InlineStyleHelper()
    {
        #region sample_step_inlinestyle_helper
        var config = new Config();
        // InlineStyle reads the style attribute for use in your own steps.
        config.Preprocess.RemoveWhere(e => InlineStyle.Get(e, "color") == "red");

        var markdown = new Converter(config).Convert(
            "<p style=\"color:red\">Dropped.</p><p style=\"color:blue\">Kept.</p>");
        // Kept.
        #endregion
        return markdown;
    }

    // ---- Table cells ----

    public static string SimplifyTableCellHtml()
    {
        #region sample_step_simplifytablecellhtml
        var config = new Config();
        config.Preprocess.SimplifyTableCellHtml();

        // A list inside a table cell has no Markdown form, so it is kept as raw HTML. This trims
        // that retained HTML down to its structure.
        var markdown = new Converter(config).Convert(
            "<table><tr><th>Policy</th></tr><tr><td>" +
            "<ol class=\"customList\"><li><p class=\"noSpacing\">" +
            "<span style=\"font-size:17px\">First point</span></p></li>" +
            "<li><p><span style=\"font-size:17px\">Second point</span></p></li></ol>" +
            "</td></tr></table>");
        // | Policy |
        // | --- |
        // | <ol><li>First point</li><li>Second point</li></ol> |
        #endregion
        return markdown;
    }

    public static string TableCellScopedCleanup()
    {
        #region sample_step_tablecell_scoped
        // A different trade-off: scope any general helper to table cells with a descendant selector.
        var config = new Config();
        config.Preprocess
            .Unwrap("td span, th span")
            .RemoveAttributes("td *, th *", "class", "style", "data-*");

        var markdown = new Converter(config).Convert(
            "<table><tr><th>Policy</th></tr><tr><td>" +
            "<ol class=\"customList\"><li><p class=\"noSpacing\">" +
            "<span style=\"font-size:17px\">First point</span></p></li></ol>" +
            "</td></tr></table>");
        // | Policy |
        // | --- |
        // | <ol><li><p>First point</p></li></ol> |
        #endregion
        return markdown;
    }

    // ---- URLs ----

    public static string ResolveRelativeUrls()
    {
        #region sample_step_resolverelativeurls
        var config = new Config();
        config.Preprocess.ResolveRelativeUrls("https://example.com/guide/index.html");

        var markdown = new Converter(config).Convert(
            "<p><a href=\"/docs\">docs</a> and <img src=\"img/a.png\" alt=\"a\"> " +
            "and <a href=\"#top\">top</a></p>");
        // [docs](https://example.com/docs) and ![a](https://example.com/guide/img/a.png) and [top](#top)
        #endregion
        return markdown;
    }
}
