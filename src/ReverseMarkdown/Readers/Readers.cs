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

    /// <summary>Anchor reader for <c>a</c>.</summary>
    public sealed class AnchorReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var link = new MdLink(element.GetAttribute("href") ?? string.Empty)
            {
                SourceTag = element.LocalName,
            };

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

    /// <summary>Image reader for <c>img</c>.</summary>
    public sealed class ImageReader : IMdReader
    {
        public void Read(IElement element, ReaderContext ctx)
        {
            var image = new MdImage(element.GetAttribute("src") ?? string.Empty)
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
