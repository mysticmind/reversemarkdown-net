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
        private readonly Config _config;

        public MarkdownDomReader(Config config)
        {
            _config = config;
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

            Register("sup", new SuperscriptReader());
            Register("sub", new SubscriptReader());

            Register("ul", new ListReader(ordered: false));
            Register("ol", new ListReader(ordered: true));
            Register("li", new ListItemReader());
            Register("pre", new PreReader());
            Register("table", new TableReader());

            // Structural wrappers: bypass (emit converted content), regardless of UnknownTags.
            var bypass = new BypassReader();
            foreach (var tag in new[]
                     {
                         "div", "span", "section", "article", "header", "footer",
                         "main", "nav", "aside", "figure", "figcaption",
                     })
            {
                Register(tag, bypass);
            }
        }

        public MarkdownDocument Read(IElement root)
        {
            var document = new MarkdownDocument();
            var ctx = new ReaderContext(this, document);
            // Read the root's children directly; the root (e.g. <body>) is a structural
            // wrapper, not subject to UnknownTags handling.
            ctx.ReadChildren(root);
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
                    ReadElement(element, ctx);
                    break;
                // comments, processing instructions, doctype: ignored
            }
        }

        private void ReadElement(IElement element, ReaderContext ctx)
        {
            var tag = element.LocalName;

            // Explicit pass-through list wins over everything (mirrors v5 Lookup order).
            if (_config.PassThroughTags.Contains(tag))
            {
                EmitRaw(element, ctx);
                return;
            }

            var reader = ResolveReader(tag);
            if (reader is not null)
            {
                reader.Read(element, ctx);
                return;
            }

            switch (_config.UnknownTags)
            {
                case Config.UnknownTagsOption.PassThrough:
                    EmitRaw(element, ctx);
                    break;
                case Config.UnknownTagsOption.Drop:
                    break;
                case Config.UnknownTagsOption.Bypass:
                    ctx.ReadChildren(element);
                    break;
                case Config.UnknownTagsOption.Raise:
                    throw new UnknownTagException(tag);
            }
        }

        // Resolve a reader for a tag, following tag aliases (with a cycle guard).
        private IMdReader? ResolveReader(string tag)
        {
            if (_readers.TryGetValue(tag, out var direct))
            {
                return direct;
            }

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { tag };
            var current = tag;
            while (_config.TagAliases.TryGetValue(current, out var target) && !string.IsNullOrWhiteSpace(target))
            {
                if (!visited.Add(target))
                {
                    break;
                }

                if (_readers.TryGetValue(target, out var aliased))
                {
                    return aliased;
                }

                current = target;
            }

            return null;
        }

        // Emit verbatim HTML via the escape hatch, choosing inline vs block by context.
        private static void EmitRaw(IElement element, ReaderContext ctx)
        {
            if (ctx.CurrentAcceptsInline)
            {
                ctx.Emit(new MdRawInline(element.OuterHtml) { SourceTag = element.LocalName });
            }
            else
            {
                ctx.Emit(new MdHtmlBlock(element.OuterHtml) { SourceTag = element.LocalName });
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
