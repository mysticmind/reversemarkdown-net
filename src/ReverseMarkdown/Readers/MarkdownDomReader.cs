using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Readers
{
    /// <summary>
    /// Builds a <see cref="MarkdownDocument"/> from an AngleSharp HTML tree by dispatching each
    /// element to a registered <see cref="IMdReader"/> by tag name. Text nodes become
    /// <see cref="MdText"/>; comments are skipped; unknown tags fall back to a bypass reader
    /// (emit children only). Phase B registers a hardcoded reader set; later phases move to
    /// reflection-based discovery mirroring v5 converters.
    /// </summary>
    public sealed class MarkdownDomReader
    {
        private readonly Dictionary<string, IMdReader> _readers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IMdReader _bypass = new BypassReader();

        public MarkdownDomReader()
        {
            RegisterDefaults();
        }

        public void Register(string tag, IMdReader reader) => _readers[tag] = reader;

        private void RegisterDefaults()
        {
            for (var level = 1; level <= 6; level++)
            {
                Register("h" + level, new HeadingReader(level));
            }

            Register("p", new ParagraphReader());

            var strong = new StrongReader();
            Register("strong", strong);
            Register("b", strong);

            var em = new EmphasisReader();
            Register("em", em);
            Register("i", em);

            var strike = new StrikethroughReader();
            Register("s", strike);
            Register("del", strike);
            Register("strike", strike);

            Register("a", new AnchorReader());
            Register("img", new ImageReader());
            Register("code", new CodeReader());
            Register("br", new LineBreakReader());
            Register("hr", new ThematicBreakReader());
            Register("blockquote", new BlockquoteReader());

            Register("ul", new ListReader(ordered: false));
            Register("ol", new ListReader(ordered: true));
            Register("li", new ListItemReader());
            Register("pre", new PreReader());
        }

        public MarkdownDocument Read(IElement root)
        {
            var document = new MarkdownDocument();
            var ctx = new ReaderContext(this, document);
            ReadNode(root, ctx);
            return document;
        }

        internal void ReadNode(INode node, ReaderContext ctx)
        {
            switch (node)
            {
                case IText text:
                    ReadText(text, ctx);
                    break;
                case IElement element:
                    var reader = _readers.TryGetValue(element.LocalName, out var found) ? found : _bypass;
                    reader.Read(element, ctx);
                    break;
                // comments, processing instructions, doctype: ignored
            }
        }

        private static void ReadText(IText text, ReaderContext ctx)
        {
            // AngleSharp already decodes entities into Data.
            var value = text.Data;

            // Drop whitespace-only text between block elements; keep it inside inline content.
            if (string.IsNullOrWhiteSpace(value) && !ctx.CurrentAcceptsInline)
            {
                return;
            }

            ctx.Emit(new MdText(value) { SourceTag = "#text" });
        }
    }
}
