using AngleSharp.Dom;

namespace ReverseMarkdown.Preprocessing;

/// <summary>
/// A single transformation applied to the source HTML before it is converted. Steps run in the
/// order they were added to <see cref="HtmlPreprocessor"/>, each one seeing the result of the
/// previous step.
/// </summary>
/// <remarks>
/// Implement this to plug in a transformation that the built-in helpers on
/// <see cref="HtmlPreprocessor"/> do not cover. Implementations must be safe to call concurrently
/// when the owning <see cref="Converter"/> is used from multiple threads.
/// </remarks>
public interface IHtmlPreprocessStep
{
    /// <summary>
    /// Transforms the document in place.
    /// </summary>
    /// <param name="root">
    /// The document element (<c>&lt;html&gt;</c>) of the parsed source. Both <c>&lt;head&gt;</c>
    /// and <c>&lt;body&gt;</c> are reachable from it; use <see cref="INode.Owner"/> to create new
    /// nodes. A step must not remove the root itself.
    /// </param>
    void Apply(IElement root);
}

/// <summary>
/// A transformation applied to the raw HTML text, before it is parsed. Use this for fixups the DOM
/// cannot express because the parser would already have discarded or reinterpreted the markup;
/// everything else belongs in an <see cref="IHtmlPreprocessStep"/>.
/// </summary>
public interface IHtmlTextPreprocessStep
{
    /// <summary>
    /// Returns the transformed HTML. Line endings are normalized to <c>\n</c> before this runs.
    /// </summary>
    string Apply(string html);
}
