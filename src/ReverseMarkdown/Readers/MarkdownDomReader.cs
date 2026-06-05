using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Readers
{
    /// <summary>
    /// Builds a <see cref="MarkdownDocument"/> from an HTML tree by dispatching each node to
    /// a registered <see cref="IMdReader"/> by tag name. Unknown tags fall back to a bypass
    /// reader (emit children only). Phase A registers a hardcoded reader set; later phases
    /// move to reflection-based discovery mirroring v5 converters.
    /// </summary>
    public sealed class MarkdownDomReader
    {
        private readonly Dictionary<string, IMdReader> _readers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IMdReader _text = new TextReader();
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

        public MarkdownDocument Read(HtmlNode root)
        {
            var document = new MarkdownDocument();
            var ctx = new ReaderContext(this, document);
            ReadNode(root, ctx);
            return document;
        }

        internal void ReadNode(HtmlNode node, ReaderContext ctx)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                _text.Read(node, ctx);
                return;
            }

            if (node.NodeType == HtmlNodeType.Comment)
            {
                return;
            }

            var reader = _readers.TryGetValue(node.Name, out var found) ? found : _bypass;
            reader.Read(node, ctx);
        }
    }
}
