using System;
using System.Linq;
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
            var heading = new MdHeading(_level) { SourceTag = element.LocalName };
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

    /// <summary>Anchor reader for <c>a</c> (scheme whitelist + smart-href handling).</summary>
    public sealed class AnchorReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var href = element.GetAttribute("href") ?? string.Empty;
            var config = ctx.Config;

            // Scheme not whitelisted: bypass to plain content (v5 behavior).
            if (!config.IsSchemeWhitelisted(UrlHelper.GetScheme(href)))
            {
                ctx.ReadChildren(element);
                return;
            }

            // Smart href: when the visible text equals the href, drop the link wrapper.
            if (config.SmartHrefHandling && UrlHelper.TextMatchesHref(element.TextContent.Trim(), href))
            {
                ctx.ReadChildren(element);
                return;
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

    /// <summary>Image reader for <c>img</c> (scheme whitelist + base64 handling).</summary>
    public sealed class ImageReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var src = element.GetAttribute("src") ?? string.Empty;
            var config = ctx.Config;

            var isBase64 = src.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
            if (isBase64)
            {
                // SaveToFile is not yet supported on the v6 path; Skip (and SaveToFile without
                // a directory) drop the image, matching v5.
                if (config.Base64Images != Config.Base64ImageHandling.Include)
                {
                    return;
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
    }

    /// <summary>URL scheme / smart-href helpers shared by the anchor and image readers.</summary>
    internal static class UrlHelper
    {
        public static string GetScheme(string url)
        {
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

    /// <summary>Inline code reader for <c>code</c> (outside <c>pre</c>).</summary>
    public sealed class CodeReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            ctx.Emit(new MdInlineCode(element.TextContent) { SourceTag = element.LocalName });
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
        public void Read(IElement element, ReaderContext ctx)
        {
            var codeNode = element.QuerySelector("code") ?? element;

            ctx.Emit(new MdCodeBlock(codeNode.TextContent)
            {
                SourceTag = element.LocalName,
                Language = DetectLanguage(codeNode),
            });
        }

        private static string? DetectLanguage(IElement codeNode)
        {
            var cls = codeNode.GetAttribute("class") ?? string.Empty;
            foreach (var token in cls.Split(' '))
            {
                if (token.StartsWith("language-", StringComparison.OrdinalIgnoreCase))
                {
                    return token.Substring("language-".Length);
                }

                if (token.StartsWith("lang-", StringComparison.OrdinalIgnoreCase))
                {
                    return token.Substring("lang-".Length);
                }
            }

            return null;
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
                    var cells = tr.Children.Where(c => c.LocalName is "td" or "th").ToList();
                    if (cells.Count == 0)
                    {
                        continue;
                    }

                    var inThead = tr.ParentElement?.LocalName == "thead";
                    var row = new MdTableRow
                    {
                        SourceTag = tr.LocalName,
                        IsHeader = inThead || cells.All(c => c.LocalName == "th"),
                    };

                    using (ctx.Open(row))
                    {
                        foreach (var cell in cells)
                        {
                            var mdCell = new MdTableCell
                            {
                                SourceTag = cell.LocalName,
                                Align = ParseAlign(cell),
                            };

                            using (ctx.Open(mdCell))
                            {
                                ctx.ReadChildren(cell);
                            }

                            ctx.Emit(mdCell);
                        }
                    }

                    ctx.Emit(row);
                }
            }

            ctx.Emit(table);
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
    /// Default reader for tags with no specific reader (e.g. <c>body</c>, wrapper
    /// <c>div</c>s): bypass the wrapper and read its children into the current container.
    /// </summary>
    public sealed class BypassReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx) => ctx.ReadChildren(element);
    }
}
