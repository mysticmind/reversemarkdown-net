using System;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Readers
{
    /// <summary>Heading reader for <c>h1</c>–<c>h6</c>.</summary>
    public sealed class HeadingReader : IMdReader
    {
        private readonly int _level;

        public HeadingReader(int level)
        {
            _level = level;
        }

        public void Read(IElement element, ReaderContext ctx)
        {
            var heading = new MdHeading(_level)
            {
                SourceTag = element.LocalName,
                Attributes = AttributeHelper.From(element),
            };
            using (ctx.Open(heading))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(heading);
        }
    }

    /// <summary>Paragraph reader for <c>p</c>.</summary>
    public sealed class ParagraphReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var paragraph = new MdParagraph { SourceTag = element.LocalName };
            using (ctx.Open(paragraph))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(paragraph);
        }
    }

    /// <summary>Strong reader for <c>strong</c> / <c>b</c>.</summary>
    public sealed class StrongReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var strong = new MdStrong { SourceTag = element.LocalName };
            using (ctx.Open(strong))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(strong);
        }
    }

    /// <summary>Emphasis reader for <c>em</c> / <c>i</c>.</summary>
    public sealed class EmphasisReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var em = new MdEmphasis { SourceTag = element.LocalName };
            using (ctx.Open(em))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(em);
        }
    }

    /// <summary>Strikethrough reader for <c>s</c> / <c>del</c> / <c>strike</c>.</summary>
    public sealed class StrikethroughReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var strike = new MdStrikethrough { SourceTag = element.LocalName };
            using (ctx.Open(strike))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(strike);
        }
    }

    /// <summary>Anchor reader for <c>a</c> (footnotes + scheme whitelist + smart-href handling).</summary>
    public sealed class AnchorReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var href = element.GetAttribute("href") ?? string.Empty;
            var cls = element.GetAttribute("class") ?? string.Empty;

            // Footnote back-reference (↩ link inside a definition): suppress entirely.
            if (cls.Contains("footnote-back") || href.StartsWith("#fnref", StringComparison.OrdinalIgnoreCase)
                || FootnoteHelper.IsBackArrow(element.TextContent))
            {
                return;
            }

            // Footnote reference: <a class="footnote-ref" href="#fn1"><sup>1</sup></a>.
            if (cls.Contains("footnote-ref") || href.StartsWith("#fn", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Emit(new MdFootnoteReference(FootnoteHelper.KeyFrom(href)) { SourceTag = element.LocalName });
                return;
            }

            var config = ctx.Config;

            // Scheme not whitelisted: bypass to plain content (v5 behavior).
            if (!config.IsSchemeWhitelisted(UrlHelper.GetScheme(href)))
            {
                ctx.ReadChildren(element);
                return;
            }

            // Smart href handling (v5 parity), only for a well-formed URL with a scheme.
            if (config.SmartHrefHandling && UrlHelper.GetScheme(href).Length > 0 &&
                Uri.IsWellFormedUriString(href, UriKind.RelativeOrAbsolute))
            {
                var text = element.TextContent.Trim();
                var scheme = UrlHelper.GetScheme(href);

                // Text already equals the href (or its tel:/mailto: form): drop the link, keep text.
                if (href.Equals(text, StringComparison.OrdinalIgnoreCase) ||
                    href.Equals("tel:" + text, StringComparison.OrdinalIgnoreCase) ||
                    href.Equals("mailto:" + text, StringComparison.OrdinalIgnoreCase))
                {
                    ctx.ReadChildren(element);
                    return;
                }

                // http(s) link whose text is the href minus the scheme: output the full href.
                if (scheme is "http" or "https" &&
                    href.Equals($"{scheme}://{text}", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Emit(new MdText(href) { SourceTag = element.LocalName });
                    return;
                }
            }

            var link = new MdLink(href) { SourceTag = element.LocalName };
            var title = element.GetAttribute("title");
            if (!string.IsNullOrEmpty(title))
            {
                link.Title = title;
            }

            using (ctx.Open(link))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(link);
        }
    }

    /// <summary>
    /// Figure reader for <c>figure</c>: a figure wrapping an image (with a figcaption that just
    /// repeats the alt, as MMD/Pandoc emit) converts to the image alone; otherwise bypass.
    /// </summary>
    public sealed class FigureReader : IMdReader
    {
        private static readonly ImageReader Image = new();

        public void Read(IElement element, ReaderContext ctx)
        {
            var img = element.QuerySelector("img");
            var figcaption = element.QuerySelector("figcaption");
            if (img is not null && figcaption is not null)
            {
                // Build the image into a throwaway holder so we can attach the figcaption before
                // emitting. The figcaption carries the rendered markdown of the original alt
                // (MMD/Pandoc figure behavior), so it is the faithful alt source.
                var holder = new MdParagraph();
                using (ctx.Open(holder))
                {
                    Image.Read(img, ctx);
                }

                var image = holder.Children.OfType<MdImage>().FirstOrDefault();
                if (image is not null)
                {
                    var caption = new MdParagraph();
                    var previous = ctx.ForceMarkdownInline;
                    ctx.ForceMarkdownInline = true;
                    using (ctx.Open(caption))
                    {
                        ctx.ReadChildren(figcaption);
                    }

                    ctx.ForceMarkdownInline = previous;
                    image.CaptionInlines = caption.Children.ToList();
                    ctx.Emit(image);
                    return;
                }
            }

            // <figure> is block-level: isolate its content as its own block.
            ctx.FlushImplicitParagraph();
            ctx.ReadChildren(element);
            ctx.FlushImplicitParagraph();
        }
    }

    /// <summary>Image reader for <c>img</c> (scheme whitelist + base64 handling).</summary>
    public sealed class ImageReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var config = ctx.Config;
            var src = ResolveSource(element, config);

            // Only a well-formed image data URI counts as base64; a malformed/truncated data: URI
            // falls through to the scheme whitelist check (its scheme is "data"), matching v5.
            var isBase64 = ImageUtils.IsValidBase64ImageData(src);
            if (isBase64)
            {
                switch (config.Base64Images)
                {
                    case Config.Base64ImageHandling.Skip:
                        return;
                    case Config.Base64ImageHandling.SaveToFile:
                        var saved = Base64ImageWriter.Save(src, config, ctx);
                        if (saved is null)
                        {
                            return; // no directory / decode failure -> drop, matching v5
                        }

                        src = saved;
                        break;
                    // Include: keep the data URI as-is
                }
            }
            else if (!config.IsSchemeWhitelisted(UrlHelper.GetScheme(src)))
            {
                // Non-whitelisted image scheme: drop (v5 emits empty output).
                return;
            }

            var image = new MdImage(src)
            {
                SourceTag = element.LocalName,
                Alt = element.GetAttribute("alt") ?? string.Empty,
            };

            var title = element.GetAttribute("title");
            if (!string.IsNullOrEmpty(title))
            {
                image.Title = title;
            }

            ctx.Emit(image);
        }

        /// <summary>
        /// Returns the image source. When <see cref="Config.LazyImageSrcFallback"/> is enabled and the
        /// <c>src</c> is empty or a <c>data:</c> placeholder (the fingerprint of JavaScript lazy-loading),
        /// the first usable URL from <see cref="Config.LazyImageSourceAttributes"/> is used instead.
        /// </summary>
        private static string ResolveSource(IElement element, Config config)
        {
            var src = element.GetAttribute("src") ?? string.Empty;

            if (!config.LazyImageSrcFallback || !IsPlaceholderSource(src))
            {
                return src;
            }

            foreach (var attributeName in config.LazyImageSourceAttributes)
            {
                var candidate = ExtractFirstUrl(element.GetAttribute(attributeName) ?? string.Empty);
                if (!string.IsNullOrEmpty(candidate) && !IsPlaceholderSource(candidate))
                {
                    return candidate;
                }
            }

            return src;
        }

        private static bool IsPlaceholderSource(string src)
        {
            return string.IsNullOrWhiteSpace(src) ||
                   src.TrimStart().StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        }

        // A srcset-style value is a comma-separated list of "url [descriptor]" candidates;
        // take the first URL. For a plain src value this returns it unchanged.
        private static string ExtractFirstUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var firstCandidate = value.Split(',')[0].Trim();
            var spaceIndex = firstCandidate.IndexOf(' ');
            return spaceIndex < 0 ? firstCandidate : firstCandidate.Substring(0, spaceIndex);
        }
    }

    /// <summary>Decodes and saves a base64 data-URI image to disk, returning the saved filename.</summary>
    internal static class Base64ImageWriter
    {
        public static string? Save(string dataUri, Config config, ReaderContext ctx)
        {
            if (string.IsNullOrEmpty(config.Base64ImageSaveDirectory))
            {
                return null;
            }

            var comma = dataUri.IndexOf(',');
            if (comma < 0 || comma <= 5)
            {
                return null;
            }

            var meta = dataUri.Substring(5, comma - 5); // e.g. "image/png;base64"
            var mime = meta.Split(';')[0];

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(dataUri.Substring(comma + 1));
            }
            catch (FormatException)
            {
                return null;
            }

            var index = ctx.NextImageIndex();
            var name = config.Base64ImageFileNameGenerator?.Invoke(index, mime) ?? $"image_{index}";
            var fileName = name + "." + ExtensionFor(mime);

            System.IO.Directory.CreateDirectory(config.Base64ImageSaveDirectory);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(config.Base64ImageSaveDirectory, fileName), bytes);
            return fileName;
        }

        private static string ExtensionFor(string mime) => mime.ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/jpeg" or "image/jpg" => "jpg",
            "image/gif" => "gif",
            "image/webp" => "webp",
            "image/svg+xml" => "svg",
            "image/bmp" => "bmp",
            _ => "img",
        };
    }

    /// <summary>URL scheme / smart-href helpers shared by the anchor and image readers.</summary>
    internal static class UrlHelper
    {
        public static string GetScheme(string url)
        {
            // Per RFC 3986 ambiguity handling (matching v5): a protocol-relative URL (//host) is
            // treated as http, and a Unix-style absolute path (/path) as file, so scheme whitelists
            // can allow/deny them.
            if (url.StartsWith("//", StringComparison.Ordinal) && url.Length > 2 && url[2] != '/')
            {
                return "http";
            }

            if (url.StartsWith("/", StringComparison.Ordinal) && url.Length > 1 && url[1] != '/')
            {
                return "file";
            }

            var colon = url.IndexOf(':');
            if (colon <= 0)
            {
                return string.Empty;
            }

            var slash = url.IndexOf('/');
            if (slash >= 0 && slash < colon)
            {
                return string.Empty;
            }

            var scheme = url.Substring(0, colon);
            foreach (var c in scheme)
            {
                if (!char.IsLetterOrDigit(c) && c is not ('+' or '-' or '.'))
                {
                    return string.Empty;
                }
            }

            return scheme;
        }

        public static bool TextMatchesHref(string text, string href)
        {
            if (string.Equals(text, href, StringComparison.Ordinal))
            {
                return true;
            }

            // http(s)://text == href, or mailto:/tel: prefix removed equals text.
            foreach (var prefix in new[] { "http://", "https://", "mailto:", "tel:" })
            {
                if (href.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(href.Substring(prefix.Length), text, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>Inline code reader for <c>code</c> (or inline math when class contains "math").</summary>
    public sealed class CodeReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var cls = element.GetAttribute("class") ?? string.Empty;
            if (cls.Contains("math"))
            {
                ctx.Emit(MathHelper.Build(element, cls));
                return;
            }

            ctx.Emit(new MdInlineCode(element.TextContent) { SourceTag = element.LocalName });
        }
    }

    /// <summary>Span reader: math (class "math") today; otherwise bypass to children.</summary>
    public sealed class SpanReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var cls = element.GetAttribute("class") ?? string.Empty;
            if (cls.Contains("math"))
            {
                ctx.Emit(MathHelper.Build(element, cls));
                return;
            }

            // Pandoc bracketed span: a classed/id'd span carries attributes.
            var attributes = AttributeHelper.From(element);
            if (attributes is not null)
            {
                var span = new MdBracketedSpan { SourceTag = element.LocalName, Attributes = attributes };
                using (ctx.Open(span))
                {
                    ctx.ReadChildren(element);
                }

                ctx.Emit(span);
                return;
            }

            ctx.ReadChildren(element);
        }
    }

    /// <summary>Abbreviation reader for <c>abbr</c>: emits the text and collects the title (MMD).</summary>
    public sealed class AbbrReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var title = element.GetAttribute("title");
            var text = element.TextContent.Trim();
            if (!string.IsNullOrEmpty(title) && text.Length > 0)
            {
                ctx.Document.Meta.Abbreviations[text] = title;
            }

            ctx.ReadChildren(element);
        }
    }

    /// <summary>Line break reader for <c>br</c>.</summary>
    public sealed class LineBreakReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            ctx.Emit(new MdLineBreak { SourceTag = element.LocalName });
        }
    }

    /// <summary>Thematic break reader for <c>hr</c>.</summary>
    public sealed class ThematicBreakReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            ctx.Emit(new MdThematicBreak { SourceTag = element.LocalName });
        }
    }

    /// <summary>Block quote reader for <c>blockquote</c>.</summary>
    public sealed class BlockquoteReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var quote = new MdBlockquote { SourceTag = element.LocalName };
            using (ctx.Open(quote))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(quote);
        }
    }

    /// <summary>List reader for <c>ul</c> / <c>ol</c>.</summary>
    public sealed class ListReader : IMdReader
    {
        private readonly bool _ordered;

        public ListReader(bool ordered)
        {
            _ordered = ordered;
        }

        public void Read(IElement element, ReaderContext ctx)
        {
            var list = new MdList { Ordered = _ordered, SourceTag = element.LocalName };
            if (_ordered && int.TryParse(element.GetAttribute("start"), out var start))
            {
                list.Start = start;
            }

            // A list is "loose" when any item wraps its content in a block (a <p> child) — the
            // canonical HTML signal for loose CommonMark lists.
            list.Tight = true;
            foreach (var li in element.Children)
            {
                if (li.LocalName != "li")
                {
                    continue;
                }

                foreach (var child in li.Children)
                {
                    if (child.LocalName == "p")
                    {
                        list.Tight = false;
                        break;
                    }
                }

                if (!list.Tight)
                {
                    break;
                }
            }

            using (ctx.Open(list))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(list);
        }
    }

    /// <summary>List item reader for <c>li</c>.</summary>
    public sealed class ListItemReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var item = new MdListItem { SourceTag = element.LocalName };

            // Task list item: a leading <input type="checkbox"> becomes [ ] / [x].
            var firstElement = element.Children.FirstOrDefault();
            if (firstElement is { LocalName: "input" } &&
                string.Equals(firstElement.GetAttribute("type"), "checkbox", StringComparison.OrdinalIgnoreCase))
            {
                item.Checked = firstElement.HasAttribute("checked");
                firstElement.Remove();
            }

            using (ctx.Open(item))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(item);
        }
    }

    /// <summary>Code block reader for <c>pre</c> (and <c>pre&gt;code</c>).</summary>
    public sealed class PreReader : IMdReader
    {
        // Common highlighter class conventions (v5 parity): highlight-source-json,
        // language-json, highlight-json, "brush: json". The language is group 2.
        private static readonly Regex HighlightClassRegex = new(
            @"(highlight-source-|language-|highlight-|brush:\s)([^\s]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public void Read(IElement element, ReaderContext ctx)
        {
            // Treat <pre> (or <pre><code>) content as normal HTML rather than a code block.
            if (ctx.Config.ConvertPreContentAsHtml)
            {
                ctx.ReadChildren(element.QuerySelector("code") ?? element);
                return;
            }

            var codeNode = element.QuerySelector("code") ?? element;

            var language = DetectLanguage(element, codeNode);
            var languageIsAttribute = false;
            if (language is null)
            {
                language = element.GetAttribute("language");
                languageIsAttribute = language is not null;
            }

            // Last resort: the configured default GFM language.
            if (string.IsNullOrEmpty(language))
            {
                language = ctx.Config.DefaultCodeBlockLanguage;
                languageIsAttribute = false;
            }

            ctx.Emit(new MdCodeBlock(codeNode.TextContent)
            {
                SourceTag = element.LocalName,
                Language = language,
                LanguageIsAttribute = languageIsAttribute,
            });
        }

        private static string? DetectLanguage(IElement pre, IElement codeNode)
        {
            // Highlighter conventions, in v5's lookup order: the code/pre class, the parent
            // (GitHub <div class="highlight highlight-source-json">), then a child <code>.
            foreach (var candidate in new[] { codeNode, pre, pre.ParentElement, pre.QuerySelector("code") })
            {
                var cls = candidate?.GetAttribute("class");
                if (string.IsNullOrEmpty(cls))
                {
                    continue;
                }

                var match = HighlightClassRegex.Match(cls);
                if (match.Success)
                {
                    return match.Groups[2].Value.TrimEnd(';');
                }
            }

            // Fallbacks the regex doesn't cover: a lang- prefix, and a bare single-token class
            // (e.g. <code class="ruby">; an info string may be any non-whitespace run). Exclude
            // tokens with fence-breaking delimiters (backtick/quote/angle bracket).
            var codeCls = codeNode.GetAttribute("class") ?? string.Empty;
            var tokens = codeCls.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.StartsWith("lang-", StringComparison.OrdinalIgnoreCase))
                {
                    return token.Substring("lang-".Length);
                }
            }

            if (tokens.Length == 1 && tokens[0].IndexOfAny(new[] { '`', '"', '\'', '<', '>' }) < 0)
            {
                return tokens[0];
            }

            return null;
        }
    }

    /// <summary>Citation reader for <c>cite</c> (uses <c>data-cite</c> as the key when present).</summary>
    public sealed class CitationReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var citation = new MdCitation
            {
                SourceTag = element.LocalName,
                Key = element.GetAttribute("data-cite"),
            };

            using (ctx.Open(citation))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(citation);
        }
    }

    /// <summary>Superscript reader for <c>sup</c>.</summary>
    public sealed class SuperscriptReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var sup = new MdSuperscript { SourceTag = element.LocalName };
            using (ctx.Open(sup))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(sup);
        }
    }

    /// <summary>Subscript reader for <c>sub</c>.</summary>
    public sealed class SubscriptReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var sub = new MdSubscript { SourceTag = element.LocalName };
            using (ctx.Open(sub))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(sub);
        }
    }

    /// <summary>Definition list reader for <c>dl</c>.</summary>
    public sealed class DefinitionListReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var list = new MdDefinitionList { SourceTag = element.LocalName };
            using (ctx.Open(list))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(list);
        }
    }

    /// <summary>Definition term reader for <c>dt</c>.</summary>
    public sealed class DefinitionTermReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var term = new MdDefinitionTerm { SourceTag = element.LocalName };
            using (ctx.Open(term))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(term);
        }
    }

    /// <summary>Definition description reader for <c>dd</c>.</summary>
    public sealed class DefinitionDescriptionReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var desc = new MdDefinitionDescription { SourceTag = element.LocalName };
            using (ctx.Open(desc))
            {
                ctx.ReadChildren(element);
            }

            ctx.Emit(desc);
        }
    }

    /// <summary>Table reader for <c>table</c>. Drives rows/cells explicitly (pipe table model).</summary>
    public sealed class TableReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var table = new MdTable { SourceTag = element.LocalName };

            var caption = element.QuerySelector("caption");
            if (caption is not null)
            {
                var text = caption.TextContent.Trim();
                if (text.Length > 0)
                {
                    table.Caption = text;
                }
            }

            using (ctx.Open(table))
            {
                foreach (var tr in element.QuerySelectorAll("tr"))
                {
                    // QuerySelectorAll is recursive; skip rows that belong to a nested table
                    // (they're emitted as raw HTML inside their cell, not as rows of this table).
                    if (tr.Closest("table") != element)
                    {
                        continue;
                    }

                    var hasCells = false;
                    var allHeaderCells = true;
                    foreach (var cell in tr.Children)
                    {
                        if (cell.LocalName is not ("td" or "th"))
                        {
                            continue;
                        }

                        hasCells = true;
                        allHeaderCells &= cell.LocalName == "th";
                    }

                    if (!hasCells)
                    {
                        continue;
                    }

                    var inThead = tr.ParentElement?.LocalName == "thead";
                    var row = new MdTableRow
                    {
                        SourceTag = tr.LocalName,
                        IsHeader = inThead || allHeaderCells,
                    };

                    using (ctx.Open(row))
                    {
                        foreach (var cell in tr.Children)
                        {
                            if (cell.LocalName is not ("td" or "th"))
                            {
                                continue;
                            }

                            // A header cell spanning columns is repeated once per spanned column
                            // (v5 TableHeaderColumnSpanHandling); re-read its content per copy so the
                            // cells are independent nodes.
                            var span = ctx.Config.TableHeaderColumnSpanHandling && cell.LocalName == "th"
                                ? Math.Max(1, ParseColSpan(cell))
                                : 1;

                            for (var s = 0; s < span; s++)
                            {
                                var mdCell = new MdTableCell
                                {
                                    SourceTag = cell.LocalName,
                                    Align = ParseAlign(cell),
                                };

                                using (ctx.Open(mdCell))
                                {
                                    var wasInCell = ctx.InTableCell;
                                    ctx.InTableCell = true;
                                    ctx.ReadChildren(cell);
                                    ctx.InTableCell = wasInCell;
                                }

                                ctx.Emit(mdCell);
                            }
                        }
                    }

                    ctx.Emit(row);
                }
            }

            ctx.Emit(table);
        }

        private static int ParseColSpan(IElement cell)
        {
            var raw = cell.GetAttribute("colspan");
            return int.TryParse(raw, out var span) && span > 0 ? span : 1;
        }

        private static ColumnAlignment ParseAlign(IElement cell)
        {
            var align = cell.GetAttribute("align");
            if (string.IsNullOrEmpty(align))
            {
                var style = cell.GetAttribute("style") ?? string.Empty;
                var idx = style.IndexOf("text-align", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    align = style.Substring(idx + "text-align".Length).TrimStart(':', ' ');
                }
            }

            if (string.IsNullOrEmpty(align))
            {
                return ColumnAlignment.None;
            }

            if (align.StartsWith("center", StringComparison.OrdinalIgnoreCase))
            {
                return ColumnAlignment.Center;
            }

            if (align.StartsWith("right", StringComparison.OrdinalIgnoreCase))
            {
                return ColumnAlignment.Right;
            }

            if (align.StartsWith("left", StringComparison.OrdinalIgnoreCase))
            {
                return ColumnAlignment.Left;
            }

            return ColumnAlignment.None;
        }
    }

    /// <summary>
    /// Reader for <c>section</c> / <c>div</c>: collects a footnotes section into the document
    /// meta (emitted at the end), otherwise bypasses to its children.
    /// </summary>
    public sealed class SectionReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var cls = element.GetAttribute("class") ?? string.Empty;
            var role = element.GetAttribute("role") ?? string.Empty;

            if (cls.Contains("footnotes") || role == "doc-endnotes")
            {
                foreach (var li in element.QuerySelectorAll("li[id]"))
                {
                    var key = FootnoteHelper.KeyFrom(li.GetAttribute("id") ?? string.Empty);
                    if (key.Length == 0)
                    {
                        continue;
                    }

                    var definition = new MdFootnoteDefinition(key) { SourceTag = li.LocalName };
                    using (ctx.Open(definition))
                    {
                        ctx.ReadChildren(li); // back-reference links are suppressed by AnchorReader
                    }

                    ctx.Document.Meta.Footnotes.Add(definition);
                }

                return;
            }

            // CommonMark: a <div> is a raw HTML block — emit it verbatim.
            if (element.LocalName == "div" && Config.IsCommonMarkBased(ctx.Config.Flavor))
            {
                ctx.Emit(new MdHtmlBlock(element.OuterHtml) { SourceTag = element.LocalName });
                return;
            }

            // Pandoc line block: <div class="line-block">a<br>b</div>.
            if (cls.Contains("line-block"))
            {
                var lineBlock = new MdLineBlock { SourceTag = element.LocalName };
                using (ctx.Open(lineBlock))
                {
                    ctx.ReadChildren(element);
                }

                ctx.Emit(lineBlock);
                return;
            }

            // MultiMarkdown has no fenced divs — preserve any <div> as a raw HTML block.
            if (element.LocalName == "div" && ctx.Config.Flavor == Config.MarkdownFlavor.MultiMarkdown)
            {
                ctx.Emit(new MdHtmlBlock(element.OuterHtml) { SourceTag = element.LocalName });
                return;
            }

            // Pandoc fenced div: a classed/id'd div/section carries attributes.
            var attributes = AttributeHelper.From(element);
            if (attributes is not null)
            {
                var div = new MdFencedDiv { SourceTag = element.LocalName, Attributes = attributes };
                using (ctx.Open(div))
                {
                    ctx.ReadChildren(element);
                }

                ctx.Emit(div);
                return;
            }

            // Pandoc preserves an unclassed <div> as a raw HTML block.
            if (element.LocalName == "div" && ctx.Config.Flavor == Config.MarkdownFlavor.Pandoc)
            {
                ctx.Emit(new MdHtmlBlock(element.OuterHtml) { SourceTag = element.LocalName });
                return;
            }

            // SuppressDivNewlines: each <div> contributes a soft line break instead of a block
            // break, so a run of divs renders as single-newline-separated lines.
            if (element.LocalName == "div" && ctx.Config.SuppressDivNewlines)
            {
                ctx.ReadChildren(element);
                ctx.Emit(new MdLineBreak { Hard = false, SourceTag = element.LocalName });
                return;
            }

            // div/section are block-level: isolate their content as its own block.
            ctx.FlushImplicitParagraph();
            ctx.ReadChildren(element);
            ctx.FlushImplicitParagraph();
        }
    }

    /// <summary>Builds <see cref="MdAttributes"/> (id + classes) from an element, or null if none.</summary>
    internal static class AttributeHelper
    {
        public static MdAttributes? From(IElement element)
        {
            var id = element.GetAttribute("id");
            var cls = element.GetAttribute("class");
            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(cls))
            {
                return null;
            }

            var attributes = new MdAttributes();
            if (!string.IsNullOrEmpty(id))
            {
                attributes.Id = id;
            }

            if (!string.IsNullOrEmpty(cls))
            {
                foreach (var token in cls.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    attributes.Classes.Add(token);
                }
            }

            return attributes;
        }
    }

    /// <summary>
    /// Default reader for tags with no specific reader (e.g. <c>body</c>, wrapper
    /// <c>div</c>s): bypass the wrapper and read its children into the current container.
    /// </summary>
    public sealed class BypassReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            // These wrappers (article/header/footer/main/nav/aside/figcaption) are block-level, so
            // isolate their content in its own block rather than coalescing with siblings.
            ctx.FlushImplicitParagraph();
            ctx.ReadChildren(element);
            ctx.FlushImplicitParagraph();
        }
    }

    /// <summary>
    /// Reader for tags whose content is never markdown (e.g. <c>script</c>, <c>style</c>):
    /// drop the element and its content entirely, regardless of the <c>UnknownTags</c> option.
    /// </summary>
    public sealed class DropReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            // Intentionally emit nothing and do not descend into children.
        }
    }

    /// <summary>Builds <see cref="MdMath"/> from a math element, stripping any TeX delimiters.</summary>
    internal static class MathHelper
    {
        public static MdMath Build(IElement element, string cls)
        {
            var display = cls.Contains("display");
            return new MdMath(StripDelimiters(element.TextContent.Trim()), display)
            {
                SourceTag = element.LocalName,
            };
        }

        private static string StripDelimiters(string t)
        {
            if (t.Length >= 4)
            {
                if ((t.StartsWith("\\(") && t.EndsWith("\\)")) ||
                    (t.StartsWith("\\[") && t.EndsWith("\\]")) ||
                    (t.StartsWith("$$") && t.EndsWith("$$")))
                {
                    return t.Substring(2, t.Length - 4).Trim();
                }
            }

            if (t.Length >= 2 && t.StartsWith("$") && t.EndsWith("$"))
            {
                return t.Substring(1, t.Length - 2).Trim();
            }

            return t;
        }
    }

    /// <summary>Footnote id/key helpers shared by the anchor and section readers.</summary>
    internal static class FootnoteHelper
    {
        public static bool IsBackArrow(string text)
        {
            var t = text.Trim();
            return t is "↩" or "↩︎" or "↩" or "↩︎";
        }

        // "fn1" / "#fn1" / "footnote-1" / "fnref1" -> "1"; falls back to the cleaned string.
        public static string KeyFrom(string raw)
        {
            var s = raw.TrimStart('#');
            var match = System.Text.RegularExpressions.Regex.Match(s, "\\d+");
            return match.Success ? match.Value : s;
        }
    }
}
