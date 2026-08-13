using System;
using System.Linq;
using AngleSharp.Dom;

namespace ReverseMarkdown.Preprocessing;

/// <summary>
/// Strips presentational markup that carries no meaning once the original stylesheet is gone:
/// <c>class</c>, <c>style</c> and <c>data-*</c> attributes, <c>&lt;span&gt;</c>/<c>&lt;font&gt;</c>
/// wrappers, and a <c>&lt;p&gt;</c> that is a list item's only child.
/// </summary>
/// <remarks>
/// Shared by <see cref="HtmlPreprocessor.SimplifyTableCellHtml"/>, which cleans the source document
/// before conversion, and by the reader's cell handling, which cleans a detached copy of markup it
/// is about to retain as raw HTML. Keeping one implementation keeps the two consistent.
/// </remarks>
internal static class PresentationalMarkup
{
    private static readonly string[] NoiseAttributes = ["class", "style", "data-*"];

    /// <summary>Cleans <paramref name="root"/> in place.</summary>
    /// <param name="root">The subtree to clean.</param>
    /// <param name="includeRoot">
    /// Whether to strip <paramref name="root"/>'s own attributes as well. False when the root is a
    /// table cell (whose attributes never reach the output), true when it is markup being retained
    /// verbatim (whose attributes do).
    /// </param>
    public static void Clean(IElement root, bool includeRoot)
    {
        foreach (var wrapper in root.QuerySelectorAll("span, font").ToList())
        {
            Unwrap(wrapper);
        }

        // <li><p>text</p></li> is how several editors emit list items; the paragraph carries no
        // meaning once it is the item's only content. Left alone when the item holds several
        // blocks, where it does separate them.
        foreach (var paragraph in root.QuerySelectorAll("li > p").ToList())
        {
            if (paragraph.ParentElement?.Children.Length == 1)
            {
                Unwrap(paragraph);
            }
        }

        if (includeRoot)
        {
            StripNoiseAttributes(root);
        }

        foreach (var element in root.QuerySelectorAll("*").ToList())
        {
            StripNoiseAttributes(element);
        }
    }

    private static void StripNoiseAttributes(IElement element)
    {
        foreach (var attribute in element.Attributes.ToList())
        {
            foreach (var pattern in NoiseAttributes)
            {
                var matched = pattern.EndsWith("*", StringComparison.Ordinal)
                    ? attribute.Name.StartsWith(pattern.Substring(0, pattern.Length - 1), StringComparison.OrdinalIgnoreCase)
                    : string.Equals(attribute.Name, pattern, StringComparison.OrdinalIgnoreCase);

                if (matched)
                {
                    element.RemoveAttribute(attribute.Name);
                    break;
                }
            }
        }
    }

    private static void Unwrap(IElement element)
    {
        var parent = element.Parent;
        if (parent is null)
        {
            return;
        }

        foreach (var child in element.ChildNodes.ToArray())
        {
            parent.InsertBefore(child, element);
        }

        element.Remove();
    }
}
