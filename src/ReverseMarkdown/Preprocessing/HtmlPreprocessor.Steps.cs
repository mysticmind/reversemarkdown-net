using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;

namespace ReverseMarkdown.Preprocessing;

/// <summary>
/// The built-in preprocessing steps. Every helper appends a step and returns the pipeline, so they
/// chain.
/// </summary>
public sealed partial class HtmlPreprocessor
{
    // Elements that carry meaning even with no text content, so "empty element" pruning and
    // unwrapping must not treat them as throw-away.
    private static readonly HashSet<string> VoidOrMediaElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "img", "br", "hr", "input", "iframe", "video", "audio", "embed", "object", "svg",
        "picture", "source", "canvas", "table", "math", "textarea", "select", "button"
    };

    // The document scaffolding. Steps run against the document element, so a broad selector or
    // predicate ("*", "everything without a class", "everything with display:none" - <head> computes
    // to none) sweeps these up. Detaching them leaves a document the converter cannot read, so the
    // built-in removal steps always spare them.
    private static readonly HashSet<string> StructuralElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "html", "head", "body"
    };

    private static bool IsStructural(IElement element) => StructuralElements.Contains(element.LocalName);

    // ---- Removal ----

    /// <summary>
    /// Removes every element matching <paramref name="selector"/>, along with its content.
    /// </summary>
    /// <example><c>Remove("nav, footer, .advertisement")</c></example>
    public HtmlPreprocessor Remove(string selector)
    {
        RequireSelector(selector);
        return Add($"Remove({selector})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                if (!IsStructural(element))
                {
                    element.Remove();
                }
            }
        });
    }

    /// <summary>
    /// Removes every element for which <paramref name="predicate"/> returns true, along with its
    /// content. Use this when the condition cannot be expressed as a CSS selector.
    /// </summary>
    public HtmlPreprocessor RemoveWhere(Func<IElement, bool> predicate)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        return Add("RemoveWhere", root =>
        {
            foreach (var element in root.QuerySelectorAll("*").Where(e => !IsStructural(e)).Where(predicate).ToList())
            {
                element.Remove();
            }
        });
    }

    /// <summary>
    /// Reduces the document to the elements matching <paramref name="selector"/> (in document
    /// order), dropping everything else in the body. Nested matches are kept via their outermost
    /// match rather than duplicated. If nothing matches, the document is left untouched, so a typo
    /// in the selector cannot silently empty the output.
    /// </summary>
    /// <example><c>KeepOnly("article.post")</c> to convert just the main content of a page.</example>
    public HtmlPreprocessor KeepOnly(string selector)
    {
        RequireSelector(selector);
        return Add($"KeepOnly({selector})", root =>
        {
            var matches = root.QuerySelectorAll(selector).Where(e => !IsStructural(e)).ToList();
            if (matches.Count == 0)
            {
                return;
            }

            var matchSet = new HashSet<IElement>(matches);
            var outermost = matches.Where(m => !HasAncestorIn(m, matchSet)).ToList();

            var container = root.Owner?.Body ?? root;
            foreach (var element in outermost)
            {
                element.Remove();
            }

            container.InnerHtml = string.Empty;
            foreach (var element in outermost)
            {
                container.AppendChild(element);
            }
        });
    }

    /// <summary>
    /// Removes HTML comments. Unlike <see cref="Config.FormattingOptions.RemoveComments"/>, which
    /// works on the conversion output, this drops them from the source before anything else runs.
    /// </summary>
    public HtmlPreprocessor RemoveComments()
    {
        return Add("RemoveComments", root =>
        {
            foreach (var comment in root.Descendants<IComment>().ToList())
            {
                comment.Remove();
            }
        });
    }

    /// <summary>
    /// Removes elements matching <paramref name="selector"/> that have no text and no meaningful
    /// content (images, tables, line breaks and the like). Nested wrappers collapse in one pass, so
    /// <c>&lt;div&gt;&lt;p&gt;&lt;/p&gt;&lt;/div&gt;</c> is removed entirely.
    /// </summary>
    /// <param name="selector">The elements to consider. Defaults to <c>p, div, span</c>.</param>
    public HtmlPreprocessor RemoveEmptyElements(string selector = "p, div, span")
    {
        RequireSelector(selector);
        return Add($"RemoveEmptyElements({selector})", root =>
        {
            // Document order puts a parent before its children, so walking backwards removes the
            // innermost candidates first and lets their now-empty ancestors collapse in turn.
            var candidates = root.QuerySelectorAll(selector).ToList();
            for (var i = candidates.Count - 1; i >= 0; i--)
            {
                var element = candidates[i];
                if (VoidOrMediaElements.Contains(element.LocalName) || IsStructural(element))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(element.TextContent) && !HasMeaningfulDescendant(element))
                {
                    element.Remove();
                }
            }
        });
    }

    // ---- Element and content transformation ----

    /// <summary>
    /// Renames every element matching <paramref name="selector"/> to <paramref name="tagName"/>,
    /// keeping its attributes and children.
    /// </summary>
    /// <example><c>Rename("h1", "h2")</c> to demote headings, or <c>Rename("b", "strong")</c>.</example>
    public HtmlPreprocessor Rename(string selector, string tagName)
    {
        RequireSelector(selector);
        RequireTagName(tagName);
        return Add($"Rename({selector} -> {tagName})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                var document = element.Owner;
                var parent = element.Parent;
                if (document is null || parent is null)
                {
                    continue;
                }

                var replacement = document.CreateElement(tagName);
                foreach (var attribute in element.Attributes.ToList())
                {
                    replacement.SetAttribute(attribute.Name, attribute.Value);
                }

                foreach (var child in element.ChildNodes.ToArray())
                {
                    replacement.AppendChild(child);
                }

                parent.ReplaceChild(replacement, element);
            }
        });
    }

    /// <summary>
    /// Removes every element matching <paramref name="selector"/> but keeps its children in place.
    /// </summary>
    /// <example><c>Unwrap("span, font")</c> to strip styling wrappers, or <c>Unwrap("a")</c> to
    /// keep link text without the link.</example>
    public HtmlPreprocessor Unwrap(string selector)
    {
        RequireSelector(selector);
        return Add($"Unwrap({selector})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                UnwrapElement(element);
            }
        });
    }

    private static void UnwrapElement(IElement element)
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

    /// <summary>
    /// Wraps every element matching <paramref name="selector"/> in a new
    /// <paramref name="tagName"/> element.
    /// </summary>
    /// <example><c>Wrap("table", "figure")</c></example>
    public HtmlPreprocessor Wrap(string selector, string tagName)
    {
        RequireSelector(selector);
        RequireTagName(tagName);
        return Add($"Wrap({selector} in {tagName})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                var document = element.Owner;
                var parent = element.Parent;
                if (document is null || parent is null)
                {
                    continue;
                }

                var wrapper = document.CreateElement(tagName);
                parent.InsertBefore(wrapper, element);
                wrapper.AppendChild(element);
            }
        });
    }

    /// <summary>
    /// Replaces every element matching <paramref name="selector"/> - element and content alike -
    /// with the given HTML fragment.
    /// </summary>
    /// <example><c>ReplaceWith("div.callout", "&lt;blockquote&gt;Note&lt;/blockquote&gt;")</c></example>
    public HtmlPreprocessor ReplaceWith(string selector, string html)
    {
        RequireSelector(selector);
        if (html is null)
        {
            throw new ArgumentNullException(nameof(html));
        }

        return Add($"ReplaceWith({selector})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                if (element.Parent is null)
                {
                    continue;
                }

                element.OuterHtml = html;
            }
        });
    }

    /// <summary>
    /// Replaces every element matching <paramref name="selector"/> with a text node. The text is
    /// inserted as content, not markup, so it is escaped like any other text.
    /// </summary>
    /// <example><c>ReplaceWithText("img.emoji", ":smile:")</c></example>
    public HtmlPreprocessor ReplaceWithText(string selector, string text)
    {
        RequireSelector(selector);
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        return Add($"ReplaceWithText({selector})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                var document = element.Owner;
                var parent = element.Parent;
                if (document is null || parent is null)
                {
                    continue;
                }

                parent.ReplaceChild(document.CreateTextNode(text), element);
            }
        });
    }

    /// <summary>
    /// Runs <paramref name="transform"/> against every element matching
    /// <paramref name="selector"/>. The escape hatch for anything the other helpers do not cover -
    /// rewriting attributes, moving nodes, reading content and so on.
    /// </summary>
    /// <example>
    /// <c>Transform("a[href^='/']", a =&gt; a.SetAttribute("href", "https://site.com" + a.GetAttribute("href")))</c>
    /// </example>
    public HtmlPreprocessor Transform(string selector, Action<IElement> transform)
    {
        RequireSelector(selector);
        if (transform is null)
        {
            throw new ArgumentNullException(nameof(transform));
        }

        return Add($"Transform({selector})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                transform(element);
            }
        });
    }

    /// <summary>
    /// Replaces every ordinal occurrence of <paramref name="find"/> in the document's text with
    /// <paramref name="replacement"/>. Text inside <c>&lt;script&gt;</c> and <c>&lt;style&gt;</c>
    /// is left alone; text inside <c>&lt;pre&gt;</c>/<c>&lt;code&gt;</c> is not.
    /// </summary>
    public HtmlPreprocessor ReplaceText(string find, string replacement)
    {
        if (string.IsNullOrEmpty(find))
        {
            throw new ArgumentException("Text to find is required.", nameof(find));
        }

        if (replacement is null)
        {
            throw new ArgumentNullException(nameof(replacement));
        }

        return AddTextReplacement($"ReplaceText({find})", text => text.Replace(find, replacement));
    }

    /// <summary>
    /// Replaces every <paramref name="pattern"/> match in the document's text with
    /// <paramref name="replacement"/>. Text inside <c>&lt;script&gt;</c> and <c>&lt;style&gt;</c>
    /// is left alone; text inside <c>&lt;pre&gt;</c>/<c>&lt;code&gt;</c> is not.
    /// </summary>
    public HtmlPreprocessor ReplaceText(Regex pattern, string replacement)
    {
        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        if (replacement is null)
        {
            throw new ArgumentNullException(nameof(replacement));
        }

        return AddTextReplacement($"ReplaceText(/{pattern}/)", text => pattern.Replace(text, replacement));
    }

    // ---- Attributes, styles and scripts ----

    /// <summary>
    /// Removes the named attributes from every element matching <paramref name="selector"/>. A name
    /// ending in <c>*</c> is a prefix match, so <c>"data-*"</c> removes all data attributes.
    /// </summary>
    /// <example><c>RemoveAttributes("*", "style", "class", "data-*")</c></example>
    public HtmlPreprocessor RemoveAttributes(string selector, params string[] names)
    {
        RequireSelector(selector);
        if (names is null || names.Length == 0)
        {
            throw new ArgumentException("At least one attribute name is required.", nameof(names));
        }

        var patterns = names.Where(n => !string.IsNullOrWhiteSpace(n)).ToArray();
        return Add($"RemoveAttributes({selector}: {string.Join(", ", patterns)})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                RemoveMatchingAttributes(element, patterns);
            }
        });
    }

    /// <summary>
    /// Removes inline <c>style</c> attributes.
    /// </summary>
    /// <param name="selector">The elements to clean. Defaults to every element.</param>
    public HtmlPreprocessor RemoveInlineStyles(string selector = "*")
    {
        RequireSelector(selector);
        return Add($"RemoveInlineStyles({selector})", root => RemoveInlineStyleAttributes(root, selector));
    }

    /// <summary>
    /// Removes <c>&lt;style&gt;</c> elements and stylesheet <c>&lt;link&gt;</c> elements, including
    /// the ones the HTML5 parser hoists into <c>&lt;head&gt;</c>.
    /// </summary>
    public HtmlPreprocessor RemoveStyleSheets()
    {
        return Add("RemoveStyleSheets", RemoveStyleSheetElements);
    }

    /// <summary>
    /// Removes styling wholesale: inline <c>style</c> attributes, <c>&lt;style&gt;</c> elements and
    /// stylesheet <c>&lt;link&gt;</c> elements. Equivalent to
    /// <see cref="RemoveInlineStyles"/> plus <see cref="RemoveStyleSheets"/>.
    /// </summary>
    public HtmlPreprocessor RemoveStyles()
    {
        return Add("RemoveStyles", root =>
        {
            RemoveInlineStyleAttributes(root, "*");
            RemoveStyleSheetElements(root);
        });
    }

    /// <summary>
    /// Removes <c>class</c> attributes.
    /// </summary>
    /// <param name="selector">The elements to clean. Defaults to every element.</param>
    /// <remarks>
    /// Fenced code block languages are detected from classes such as <c>language-cs</c>, so pass a
    /// selector like <c>":not(pre):not(code)"</c> if you want to keep those.
    /// </remarks>
    public HtmlPreprocessor RemoveClasses(string selector = "*")
    {
        RequireSelector(selector);
        return Add($"RemoveClasses({selector})", root =>
        {
            foreach (var element in root.QuerySelectorAll(selector).ToList())
            {
                element.RemoveAttribute("class");
            }
        });
    }

    /// <summary>
    /// Removes <c>&lt;script&gt;</c> and <c>&lt;noscript&gt;</c> elements along with inline
    /// <c>on*</c> event handler attributes.
    /// </summary>
    public HtmlPreprocessor RemoveScripts()
    {
        return Add("RemoveScripts", root =>
        {
            foreach (var element in root.QuerySelectorAll("script, noscript").ToList())
            {
                element.Remove();
            }

            foreach (var element in root.QuerySelectorAll("*").ToList())
            {
                RemoveMatchingAttributes(element, ["on*"]);
            }
        });
    }

    /// <summary>
    /// Removes elements hidden by their inline style (<c>display: none</c>,
    /// <c>visibility: hidden</c>) or by the <c>hidden</c> attribute. Email templates and CMS output
    /// are full of hidden preheader text that would otherwise land in the Markdown.
    /// </summary>
    /// <remarks>
    /// Inline styles only - a <c>display: none</c> coming from a <c>&lt;style&gt;</c> rule needs the
    /// cascade. Add <c>Remove("[aria-hidden=true]")</c> if you also want decorative content gone.
    /// </remarks>
    public HtmlPreprocessor RemoveHidden()
    {
        return Add("RemoveHidden", root =>
        {
            foreach (var element in root.QuerySelectorAll("[style], [hidden]").ToList())
            {
                if (IsStructural(element))
                {
                    continue;
                }

                if (element.HasAttribute("hidden") || InlineStyle.IsHidden(element))
                {
                    element.Remove();
                }
            }
        });
    }

    /// <summary>
    /// Turns inline formatting styles into the semantic tags Markdown can represent: a bold
    /// <c>font-weight</c> becomes <c>&lt;strong&gt;</c>, an italic <c>font-style</c> becomes
    /// <c>&lt;em&gt;</c>, and a line-through <c>text-decoration</c> becomes <c>&lt;del&gt;</c>. This
    /// recovers the formatting of Word, Outlook and Google Docs exports, which emit
    /// <c>&lt;span style="font-weight:700"&gt;</c> rather than <c>&lt;strong&gt;</c>.
    /// </summary>
    /// <remarks>
    /// The element itself is kept and its content is wrapped, so pair it with
    /// <c>Unwrap("span, font")</c> to shed the now-meaningless wrappers. Consumed declarations are
    /// stripped from the <c>style</c> attribute, which keeps the step idempotent. Inline styles
    /// only - see <see cref="InlineStyle"/> for how to handle the cascade.
    /// </remarks>
    public HtmlPreprocessor ConvertInlineStylesToTags()
    {
        return Add("ConvertInlineStylesToTags", root =>
        {
            foreach (var element in root.QuerySelectorAll("[style]").ToList())
            {
                var document = element.Owner;
                if (document is null || !element.HasChildNodes)
                {
                    continue;
                }

                // Innermost first, so the emphasis nests as <strong><em><del>text</del></em></strong>.
                if (InlineStyle.IsStruckThrough(element))
                {
                    WrapChildren(element, document, "del");
                    InlineStyle.Remove(element, "text-decoration", "text-decoration-line");
                }

                if (InlineStyle.IsItalic(element))
                {
                    WrapChildren(element, document, "em");
                    InlineStyle.Remove(element, "font-style");
                }

                if (InlineStyle.IsBold(element))
                {
                    WrapChildren(element, document, "strong");
                    InlineStyle.Remove(element, "font-weight");
                }
            }
        });
    }

    private static void WrapChildren(IElement element, IDocument document, string tagName)
    {
        var wrapper = document.CreateElement(tagName);
        foreach (var child in element.ChildNodes.ToArray())
        {
            wrapper.AppendChild(child);
        }

        element.AppendChild(wrapper);
    }

    /// <summary>
    /// Strips the presentational noise out of table cells: unwraps <c>&lt;span&gt;</c> and
    /// <c>&lt;font&gt;</c>, unwraps a <c>&lt;p&gt;</c> that is a list item's only child, and removes
    /// <c>class</c>, <c>style</c> and <c>data-*</c> attributes from everything inside a
    /// <c>&lt;td&gt;</c>/<c>&lt;th&gt;</c>.
    /// </summary>
    /// <remarks>
    /// A nested table or list inside a table cell has no Markdown representation, so it is kept as
    /// raw HTML - verbatim, which means every class, inline style and wrapper from the source comes
    /// with it. Editors such as CKEditor and SharePoint produce a lot of those. This trims the
    /// retained HTML down to its structure, which matters when the Markdown is fed to something that
    /// reads it (RAG indexing, LLM prompts) rather than rendered.
    /// <para>
    /// It only reshapes the source, so it changes nothing about which elements are retained as HTML.
    /// For a different trade-off, compose the general helpers with a cell-scoped selector, for
    /// example <c>RemoveAttributes("td *, th *", "style")</c>.
    /// </para>
    /// </remarks>
    public HtmlPreprocessor SimplifyTableCellHtml()
    {
        return Add("SimplifyTableCellHtml", root =>
        {
            foreach (var cell in root.QuerySelectorAll("td, th").ToList())
            {
                foreach (var wrapper in cell.QuerySelectorAll("span, font").ToList())
                {
                    UnwrapElement(wrapper);
                }

                // <li><p>text</p></li> is how several editors emit list items; the paragraph carries
                // no meaning once it is the item's only content, and it survives into the retained
                // HTML. Left alone when the item holds several blocks, where it does carry meaning.
                foreach (var paragraph in cell.QuerySelectorAll("li > p").ToList())
                {
                    if (paragraph.ParentElement?.Children.Length == 1)
                    {
                        UnwrapElement(paragraph);
                    }
                }

                foreach (var element in cell.QuerySelectorAll("*").ToList())
                {
                    RemoveMatchingAttributes(element, ["class", "style", "data-*"]);
                }
            }
        });
    }

    // ---- URLs ----

    /// <summary>
    /// Rewrites relative <c>href</c>, <c>src</c> and <c>poster</c> values into absolute URLs against
    /// <paramref name="baseUrl"/>, so links and images survive out of their original page. Absolute
    /// URLs (including <c>data:</c> and <c>mailto:</c>) and in-page <c>#anchor</c> references are
    /// left alone.
    /// </summary>
    public HtmlPreprocessor ResolveRelativeUrls(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new ArgumentException("An absolute base URL is required.", nameof(baseUrl));
        }

        return Add($"ResolveRelativeUrls({baseUri})", root =>
        {
            foreach (var element in root.QuerySelectorAll("[href], [src], [poster]").ToList())
            {
                ResolveAttribute(element, "href", baseUri);
                ResolveAttribute(element, "src", baseUri);
                ResolveAttribute(element, "poster", baseUri);
            }
        });
    }

    // ---- Shared implementation ----

    private HtmlPreprocessor AddTextReplacement(string name, Func<string, string> replace)
    {
        return Add(name, root =>
        {
            foreach (var node in root.Descendants<IText>().ToList())
            {
                var parent = node.ParentElement?.LocalName;
                if (string.Equals(parent, "script", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(parent, "style", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var replaced = replace(node.Data);
                if (!string.Equals(replaced, node.Data, StringComparison.Ordinal))
                {
                    node.Data = replaced;
                }
            }
        });
    }

    private static void RemoveInlineStyleAttributes(IElement root, string selector)
    {
        foreach (var element in root.QuerySelectorAll(selector).ToList())
        {
            element.RemoveAttribute("style");
        }
    }

    private static void RemoveStyleSheetElements(IElement root)
    {
        foreach (var element in root.QuerySelectorAll("style").ToList())
        {
            element.Remove();
        }

        foreach (var element in root.QuerySelectorAll("link").ToList())
        {
            var rel = element.GetAttribute("rel");
            if (rel is not null && rel.Contains("stylesheet", StringComparison.OrdinalIgnoreCase))
            {
                element.Remove();
            }
        }
    }

    private static void RemoveMatchingAttributes(IElement element, IReadOnlyList<string> patterns)
    {
        foreach (var attribute in element.Attributes.ToList())
        {
            for (var i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
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

    private static void ResolveAttribute(IElement element, string name, Uri baseUri)
    {
        var value = element.GetAttribute(name);
        if (string.IsNullOrWhiteSpace(value) || value!.StartsWith("#", StringComparison.Ordinal))
        {
            return;
        }

        // Already absolute (http:, data:, mailto:, tel: ...) - nothing to resolve. Tested by hand
        // rather than with Uri.TryCreate, which reads a root-relative "/docs" as an absolute file
        // path on Unix. Protocol-relative "//host/x" has no scheme and is resolved against the base.
        if (HasUriScheme(value))
        {
            return;
        }

        if (Uri.TryCreate(baseUri, value, out var resolved))
        {
            element.SetAttribute(name, resolved.ToString());
        }
    }

    private static bool HasUriScheme(string value)
    {
        var colon = value.IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }

        if (!char.IsLetter(value[0]))
        {
            return false;
        }

        for (var i = 1; i < colon; i++)
        {
            var c = value[i];
            if (!char.IsLetterOrDigit(c) && c != '+' && c != '-' && c != '.')
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasAncestorIn(IElement element, HashSet<IElement> candidates)
    {
        for (var parent = element.ParentElement; parent is not null; parent = parent.ParentElement)
        {
            if (candidates.Contains(parent))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasMeaningfulDescendant(IElement element)
    {
        foreach (var descendant in element.Descendants<IElement>())
        {
            if (VoidOrMediaElements.Contains(descendant.LocalName))
            {
                return true;
            }
        }

        return false;
    }

    private static void RequireSelector(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new ArgumentException("A CSS selector is required.", nameof(selector));
        }
    }

    private static void RequireTagName(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            throw new ArgumentException("A tag name is required.", nameof(tagName));
        }

        foreach (var c in tagName)
        {
            if (!char.IsLetterOrDigit(c) && c != '-' && c != '_')
            {
                throw new ArgumentException($"'{tagName}' is not a valid tag name.", nameof(tagName));
            }
        }
    }
}
