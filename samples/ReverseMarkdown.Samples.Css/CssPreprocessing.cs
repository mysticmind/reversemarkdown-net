using AngleSharp;
using AngleSharp.Dom;
using ReverseMarkdown;

namespace Samples.Css;

public static class CssPreprocessing
{
    public static string CascadeAwarePreprocessing()
    {
        #region sample_preprocess_css
        // AngleSharp.Css is an optional package that ReverseMarkdown does not depend on. Reference
        // it yourself and hand the converter a CSS-enabled browsing context; preprocessing steps
        // then get ComputeCurrentStyle(), which resolves rules from <style> blocks and not just the
        // inline style attribute.
        var context = BrowsingContext.New(Configuration.Default.WithCss());

        var config = new Config();
        config.Preprocess
            .Transform("span", span =>
            {
                if (span.ComputeCurrentStyle().GetPropertyValue("font-weight") is "bold" or "700")
                {
                    span.InnerHtml = $"<strong>{span.InnerHtml}</strong>";
                }
            })
            .RemoveStyleSheets()
            .Unwrap("span");

        var converter = new Converter(config, context);

        var markdown = converter.Convert(
            """
            <style>.bold { font-weight: bold }</style>
            <p>plain <span class="bold">cascaded bold</span> here</p>
            """);
        // plain **cascaded bold** here
        #endregion
        return markdown;
    }

    public static void HiddenByStylesheet()
    {
        #region sample_preprocess_css_hidden
        var context = BrowsingContext.New(Configuration.Default.WithCss());

        var config = new Config();
        config.Preprocess
            // Read computed styles BEFORE removing anything: <head> computes to display:none and
            // holds the stylesheets, so removing hidden content first would kill the cascade for
            // later steps. The built-in removal steps never detach <html>/<head>/<body>.
            .RemoveWhere(e => e.ComputeCurrentStyle().GetPropertyValue("display") == "none")
            .RemoveStyleSheets();

        var converter = new Converter(config, context);
        #endregion
        _ = converter;
    }
}
