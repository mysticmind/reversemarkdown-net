using System.Net;
using HtmlAgilityPack;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Readers
{
    /// <summary>Text node reader: decodes HTML entities into an <see cref="MdText"/>.</summary>
    public sealed class TextReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var value = WebUtility.HtmlDecode(node.InnerText);

            // Drop whitespace-only text between block elements; keep it inside inline content.
            if (string.IsNullOrWhiteSpace(value) && !ctx.CurrentAcceptsInline)
            {
                return;
            }

            ctx.Emit(new MdText(value) { SourceTag = node.Name });
        }
    }

    /// <summary>Heading reader for <c>h1</c>–<c>h6</c>.</summary>
    public sealed class HeadingReader : IMdReader
    {
        private readonly int _level;

        public HeadingReader(int level)
        {
            _level = level;
        }

        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var heading = new MdHeading(_level) { SourceTag = node.Name };
            using (ctx.Open(heading))
            {
                ctx.ReadChildren(node);
            }

            ctx.Emit(heading);
        }
    }

    /// <summary>Paragraph reader for <c>p</c>.</summary>
    public sealed class ParagraphReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var paragraph = new MdParagraph { SourceTag = node.Name };
            using (ctx.Open(paragraph))
            {
                ctx.ReadChildren(node);
            }

            ctx.Emit(paragraph);
        }
    }

    /// <summary>Strong reader for <c>strong</c> / <c>b</c>.</summary>
    public sealed class StrongReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var strong = new MdStrong { SourceTag = node.Name };
            using (ctx.Open(strong))
            {
                ctx.ReadChildren(node);
            }

            ctx.Emit(strong);
        }
    }

    /// <summary>Emphasis reader for <c>em</c> / <c>i</c>.</summary>
    public sealed class EmphasisReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var em = new MdEmphasis { SourceTag = node.Name };
            using (ctx.Open(em))
            {
                ctx.ReadChildren(node);
            }

            ctx.Emit(em);
        }
    }

    /// <summary>Strikethrough reader for <c>s</c> / <c>del</c> / <c>strike</c>.</summary>
    public sealed class StrikethroughReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var strike = new MdStrikethrough { SourceTag = node.Name };
            using (ctx.Open(strike))
            {
                ctx.ReadChildren(node);
            }

            ctx.Emit(strike);
        }
    }

    /// <summary>Anchor reader for <c>a</c>.</summary>
    public sealed class AnchorReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var href = WebUtility.HtmlDecode(node.GetAttributeValue("href", string.Empty));
            var link = new MdLink(href) { SourceTag = node.Name };

            var title = node.GetAttributeValue("title", string.Empty);
            if (!string.IsNullOrEmpty(title))
            {
                link.Title = WebUtility.HtmlDecode(title);
            }

            using (ctx.Open(link))
            {
                ctx.ReadChildren(node);
            }

            ctx.Emit(link);
        }
    }

    /// <summary>Image reader for <c>img</c>.</summary>
    public sealed class ImageReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var src = WebUtility.HtmlDecode(node.GetAttributeValue("src", string.Empty));
            var image = new MdImage(src)
            {
                SourceTag = node.Name,
                Alt = WebUtility.HtmlDecode(node.GetAttributeValue("alt", string.Empty)),
            };

            var title = node.GetAttributeValue("title", string.Empty);
            if (!string.IsNullOrEmpty(title))
            {
                image.Title = WebUtility.HtmlDecode(title);
            }

            ctx.Emit(image);
        }
    }

    /// <summary>Inline code reader for <c>code</c> (outside <c>pre</c>).</summary>
    public sealed class CodeReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            ctx.Emit(new MdInlineCode(WebUtility.HtmlDecode(node.InnerText)) { SourceTag = node.Name });
        }
    }

    /// <summary>Line break reader for <c>br</c>.</summary>
    public sealed class LineBreakReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            ctx.Emit(new MdLineBreak { SourceTag = node.Name });
        }
    }

    /// <summary>Thematic break reader for <c>hr</c>.</summary>
    public sealed class ThematicBreakReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            ctx.Emit(new MdThematicBreak { SourceTag = node.Name });
        }
    }

    /// <summary>Block quote reader for <c>blockquote</c>.</summary>
    public sealed class BlockquoteReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx)
        {
            var quote = new MdBlockquote { SourceTag = node.Name };
            using (ctx.Open(quote))
            {
                ctx.ReadChildren(node);
            }

            ctx.Emit(quote);
        }
    }

    /// <summary>
    /// Default reader for tags with no specific reader (e.g. <c>body</c>, wrapper
    /// <c>div</c>s): bypass the wrapper and read its children into the current container.
    /// </summary>
    public sealed class BypassReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx) => ctx.ReadChildren(node);
    }
}
