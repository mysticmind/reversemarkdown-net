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

    /// <summary>
    /// Default reader for tags with no specific reader (e.g. <c>body</c>, wrapper
    /// <c>div</c>s): bypass the wrapper and read its children into the current container.
    /// </summary>
    public sealed class BypassReader : IMdReader
    {
        public void Read(HtmlNode node, ReaderContext ctx) => ctx.ReadChildren(node);
    }
}
