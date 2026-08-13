using System.Linq;
using AngleSharp.Dom;
using ReverseMarkdown;
using ReverseMarkdown.Preprocessing;

namespace Samples;

#region sample_preprocess_step_class
// A reusable step: implement IHtmlPreprocessStep and hand it to Add.
public sealed class UppercaseHeadings : IHtmlPreprocessStep
{
    public void Apply(IElement root)
    {
        foreach (var heading in root.QuerySelectorAll("h1, h2, h3"))
        {
            heading.TextContent = heading.TextContent.ToUpperInvariant();
        }
    }
}
#endregion

public static class Preprocessing
{
    private const string Html = "<p>sample</p>";

    public static void QuickStart()
    {
        #region sample_preprocess
        var config = new Config();
        config.Preprocess
            .RemoveScripts()                        // <script>/<noscript> and on* handlers
            .RemoveStyles()                         // style attributes, <style>, stylesheet <link>
            .Remove("nav, footer, .advertisement")  // drop chrome
            .Unwrap("span, font")                   // keep the text, lose the wrapper
            .Rename("b", "strong")                  // <b> -> <strong>
            .RemoveEmptyElements();                 // collapse leftover empty wrappers

        var markdown = new Converter(config).Convert(Html);
        #endregion
        _ = markdown;
    }

    public static void ExtractArticle()
    {
        #region sample_preprocess_extract
        // Convert just the article body of a scraped page, with relative links made absolute.
        var config = new Config();
        config.Preprocess
            .KeepOnly("article.post")
            .ResolveRelativeUrls("https://example.com/blog/my-post")
            .ReplaceWith("div.callout", "<blockquote>Note</blockquote>")
            .ReplaceWithText("img.emoji", ":smile:");
        #endregion
        _ = config;
    }

    public static void InlineStyles()
    {
        #region sample_preprocess_styles
        // Word, Outlook and Google Docs emit formatting as inline CSS rather than semantic tags.
        var config = new Config();
        config.Preprocess
            .RemoveHidden()                 // display:none / visibility:hidden / hidden
            .ConvertInlineStylesToTags()    // font-weight:700 -> <strong>, font-style:italic -> <em>
            .Unwrap("span, font");          // shed the now-meaningless wrappers

        // <span style="font-weight:700">bold</span>  ->  **bold**
        #endregion
        _ = config;
    }

    public static void CustomStep()
    {
        #region sample_preprocess_custom
        var config = new Config();

        // Anything the built-ins do not cover: a per-element transform...
        config.Preprocess.Transform(
            "a[href^='/']",
            a => a.SetAttribute("href", "https://example.com" + a.GetAttribute("href")));

        // ...or a whole-document step, which receives the <html> element.
        config.Preprocess.Add("drop-empty-tables", root =>
        {
            foreach (var table in root.QuerySelectorAll("table").ToList())
            {
                if (table.QuerySelector("td, th") is null)
                {
                    table.Remove();
                }
            }
        });
        #endregion
        _ = config;
    }

    public static void StepClass()
    {
        var config = new Config();
        #region sample_preprocess_step_class_usage
        config.Preprocess.Add(new UppercaseHeadings());
        #endregion
    }

    public static void TextSteps()
    {
        var config = new Config();
        #region sample_preprocess_text
        config.Preprocess
            .AddText("expand-template", html => html.Replace("{{name}}", "Jane"))
            .Remove("nav");   // DOM step, still sees the expanded markup
        #endregion
    }

    public static void InspectPreprocessedHtml()
    {
        #region sample_preprocess_inspect
        var config = new Config();
        config.Preprocess.RemoveScripts().Remove("nav");

        // The transformed HTML, without converting it.
        var cleaned = new Converter(config).Preprocess(Html);
        #endregion
        _ = cleaned;
    }
}
