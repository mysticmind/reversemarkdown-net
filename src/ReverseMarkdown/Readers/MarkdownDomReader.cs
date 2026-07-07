using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

        // Trimming/AOT-safe: registers only the built-in readers, no reflection. Add custom
        // readers with <see cref="Register"/>.
        public MarkdownDomReader(Config config)
        {
            _config = config;
            RegisterDefaults();
        }

        [RequiresUnreferencedCode("Scans the given assemblies for [MarkdownReader] reader types via reflection; those types may be removed by the trimmer. Register custom readers explicitly with Register instead.")]
        [RequiresDynamicCode("Instantiates discovered reader types via Activator.CreateInstance.")]
        public MarkdownDomReader(Config config, params Assembly[]? additionalAssemblies)
        {
            _config = config;
            RegisterDefaults();

            // Auto-discover external readers (decorated with [MarkdownReader]) from additional
            // assemblies; these override built-ins for the same tag, enabling customization.
            if (additionalAssemblies is { Length: > 0 })
            {
                RegisterFromAssemblies(additionalAssemblies);
            }
        }

        [RequiresUnreferencedCode("Scans assemblies for [MarkdownReader] reader types via reflection.")]
        [RequiresDynamicCode("Instantiates discovered reader types via Activator.CreateInstance.")]
        private void RegisterFromAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || !typeof(IMdReader).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    var attribute = type.GetCustomAttribute<MarkdownReaderAttribute>();
                    if (attribute is null || type.GetConstructor(Type.EmptyTypes) is null)
                    {
                        continue;
                    }

                    var instance = (IMdReader)Activator.CreateInstance(type)!;
                    foreach (var tag in attribute.Tags)
                    {
                        Register(tag, instance);
                    }
                }
            }
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
            Register("figure", new FigureReader());
            Register("code", new CodeReader());
            Register("br", new LineBreakReader());
            Register("hr", new ThematicBreakReader());
            Register("blockquote", new BlockquoteReader());

            Register("sup", new SuperscriptReader());
            Register("sub", new SubscriptReader());
            Register("cite", new CitationReader());
            Register("abbr", new AbbrReader());
            Register("span", new SpanReader());

            Register("ul", new ListReader(ordered: false));
            Register("ol", new ListReader(ordered: true));
            Register("li", new ListItemReader());
            Register("pre", new PreReader());
            Register("table", new TableReader());
            Register("dl", new DefinitionListReader());
            Register("dt", new DefinitionTermReader());
            Register("dd", new DefinitionDescriptionReader());

            // script/style content is never markdown: drop it (and its content) outright,
            // regardless of the UnknownTags option.
            var drop = new DropReader();
            Register("script", drop);
            Register("style", drop);

            // div/section may carry a footnotes block; the section reader handles that and
            // otherwise bypasses.
            var section = new SectionReader();
            Register("div", section);
            Register("section", section);

            // Other structural wrappers: bypass (emit converted content), regardless of UnknownTags.
            var bypass = new BypassReader();
            foreach (var tag in new[]
                     {
                         "article", "header", "footer",
                         "main", "nav", "aside", "figcaption",
                     })
            {
                Register(tag, bypass);
            }
        }

        public MarkdownDocument Read(IElement root)
        {
            var document = new MarkdownDocument();
            var ctx = new ReaderContext(this, document, _config);
            // Read the root's children directly; the root (e.g. <body>) is a structural
            // wrapper, not subject to UnknownTags handling.
            ctx.ReadChildren(root);
            ctx.FinalizeRoot();
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
                case IComment comment
                    when Config.PreservesInlineRawHtml(_config.Flavor) && !_config.Formatting.RemoveComments:
                    // CommonMark preserves HTML comments (incl. AngleSharp-normalized PIs/decls).
                    ctx.Emit(new MdRawInline("<!--" + comment.Data + "-->") { SourceTag = "#comment" });
                    break;
                // other comments, processing instructions, doctype: ignored
            }
        }

        // Inline HTML elements that CommonMark (with CommonMarkUseHtmlInlineTags) emits verbatim
        // rather than converting — matching v5, which is how the spec round-trips exactly.
        // Note: img/code are excluded — their clean markdown (![]() / `code`) round-trips more
        // faithfully than raw HTML (img always gets an alt; code-span padding is exact).
        private static readonly HashSet<string> InlineHtmlTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "strong", "b", "em", "i", "del", "ins", "s", "strike",
            "sub", "sup", "span", "abbr", "cite", "mark", "small", "q", "u", "kbd", "samp", "var",
        };

        private void ReadElement(IElement element, ReaderContext ctx)
        {
            var tag = element.LocalName;

            // Explicit pass-through list wins over everything (mirrors v5 Lookup order).
            if (_config.Tags.PassThrough.Contains(tag))
            {
                EmitRaw(element, ctx);
                return;
            }

            // A nested table or list inside a table cell has no markdown representation, so emit it
            // as compacted raw HTML (v5 leaves it as-is, single-spaced between tags).
            if (ctx.InTableCell && tag is "table" or "ol" or "ul")
            {
                ctx.Emit(new MdHtmlBlock(CompactNestedHtml(element.OuterHtml)) { SourceTag = tag });
                return;
            }

            // CommonMark inline-HTML passthrough: emit inline elements verbatim (v5 parity).
            // GFM autolinks bare URLs, so a raw <a> whose text is a URL would get re-linked by the
            // renderer; for GitHub, a text-only <a> becomes a markdown link instead (an empty <a>
            // or one with element children still passes through raw — those weird cases need it).
            // Convert <a> to a markdown link only when its text is URL-like (the case the GFM
            // autolinker would otherwise re-link); other <a> stay raw so weird hrefs round-trip.
            var anchorText = element.TextContent;
            var gfmTextOnlyAnchor = tag == "a" && _config.Flavor == Config.MarkdownFlavor.GitHub &&
                                    element.Children.Length == 0 &&
                                    !string.IsNullOrWhiteSpace(anchorText) &&
                                    (anchorText.Contains("://") || anchorText.Contains('@') || anchorText.Contains("www."));
            // MultiMarkdown converts plain emphasis (em/i/strong/b with no attributes) to clean
            // markdown so its text-derived heading ids match; only tags it lacks markdown for, or
            // ones carrying attributes, pass through raw. CommonMark/GFM pass all of them through.
            var hasCleanMmdMarkdown = tag is "em" or "i" or "strong" or "b";
            var mmdConvertsInline = _config.Flavor == Config.MarkdownFlavor.MultiMarkdown &&
                                    hasCleanMmdMarkdown && element.Attributes.Length == 0;
            var passthroughTag = InlineHtmlTags.Contains(tag) && !gfmTextOnlyAnchor && !mmdConvertsInline;
            if (Config.PreservesInlineRawHtml(_config.Flavor) &&
                !ctx.ForceMarkdownInline &&
                _config.CommonMarkUseHtmlInlineTags &&
                passthroughTag)
            {
                // For an INLINE element, keep the open/close tags raw but emit text content as
                // escaped markdown (so markdown-significant chars in the text aren't reinterpreted
                // by the renderer) and child elements raw (so nested raw HTML still round-trips).
                // A block-level element is a verbatim HTML block, so it falls through to full raw.
                if (element.ChildNodes.Length > 0 && ctx.CurrentAcceptsInline)
                {
                    ctx.Emit(new MdRawInline(StartTag(element)) { SourceTag = tag });
                    foreach (var child in element.ChildNodes)
                    {
                        switch (child)
                        {
                            case IText text:
                                ctx.Emit(new MdText(text.Data) { SourceTag = "#text" });
                                break;
                            case IElement childElement:
                                ctx.Emit(new MdRawInline(childElement.OuterHtml) { SourceTag = childElement.LocalName });
                                break;
                        }
                    }

                    ctx.Emit(new MdRawInline("</" + tag + ">") { SourceTag = tag });
                    return;
                }

                ctx.Emit(new MdRawInline(element.OuterHtml) { SourceTag = tag });
                return;
            }

            var reader = ResolveReader(tag);
            if (reader is not null)
            {
                reader.Read(element, ctx);
                return;
            }

            // Unknown-tag replacer: wrap converted content with the configured markdown string.
            if (_config.Tags.Replacer.TryGetValue(tag, out var wrapper))
            {
                ctx.Emit(new MdRawInline(wrapper) { SourceTag = tag });
                ctx.ReadChildren(element);
                ctx.Emit(new MdRawInline(wrapper) { SourceTag = tag });
                return;
            }

            switch (_config.Tags.Unknown)
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

        // Compact nested-in-table HTML to a single line (v5 CompactHtmlForMarkdown): drop line
        // endings, collapse inter-tag whitespace to one space, and strip the <tbody> the HTML5
        // parser auto-inserts (v5's HAP serialization didn't add it).
        private static string CompactNestedHtml(string html)
        {
            html = html.Replace("\r\n", string.Empty).Replace("\n", string.Empty);
            html = Regex.Replace(html, @">\s+<", "> <");
            html = html.Replace("<tbody>", string.Empty).Replace("</tbody>", string.Empty);
            return html.Trim();
        }

        // The element's serialized start tag (e.g. <a href="...">), taken verbatim from
        // AngleSharp's OuterHtml so attribute quoting/escaping is exactly preserved.
        private static string StartTag(IElement element)
        {
            var outer = element.OuterHtml;
            var closeLength = element.LocalName.Length + 3; // "</" + name + ">"
            return outer.Substring(0, outer.Length - element.InnerHtml.Length - closeLength);
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
            while (_config.Tags.Aliases.TryGetValue(current, out var target) && !string.IsNullOrWhiteSpace(target))
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

        // Inline-level HTML elements: when passed through raw at block level (e.g. a root-level
        // <img> in a PassThroughTags run), they are still inline flow and must coalesce into the
        // surrounding implicit paragraph rather than break out as their own HTML block.
        private static readonly HashSet<string> InlineLevelTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "a", "abbr", "b", "bdi", "bdo", "br", "cite", "code", "data", "dfn", "em", "i", "img",
            "input", "kbd", "label", "mark", "q", "s", "samp", "small", "span", "strong", "sub",
            "sup", "time", "u", "var", "wbr", "button", "select", "textarea",
        };

        // HTML boolean attributes serialize bare (e.g. "disabled", not disabled=""). AngleSharp's
        // OuterHtml always emits name="" form; minimize the known booleans back to bare so raw
        // passthrough matches conventional HTML (v5/HtmlAgilityPack parity).
        private static readonly HashSet<string> BooleanAttributes = new(StringComparer.OrdinalIgnoreCase)
        {
            "checked", "disabled", "selected", "readonly", "multiple", "required", "autofocus",
            "hidden", "novalidate", "formnovalidate", "async", "defer", "ismap", "loop", "muted",
            "controls", "autoplay", "default", "open", "reversed", "nomodule", "itemscope",
        };

        private static readonly Regex BooleanAttributeRegex = new(
            @"\s([a-zA-Z][a-zA-Z0-9-]*)=""""", RegexOptions.Compiled);

        private static string MinimizeBooleanAttributes(string html) =>
            BooleanAttributeRegex.Replace(html, m =>
                BooleanAttributes.Contains(m.Groups[1].Value) ? " " + m.Groups[1].Value : m.Value);

        // Emit verbatim HTML via the escape hatch, choosing inline vs block by context.
        private static void EmitRaw(IElement element, ReaderContext ctx)
        {
            var html = MinimizeBooleanAttributes(element.OuterHtml);
            if (ctx.CurrentAcceptsInline || InlineLevelTags.Contains(element.LocalName))
            {
                ctx.Emit(new MdRawInline(html) { SourceTag = element.LocalName });
            }
            else
            {
                ctx.Emit(new MdHtmlBlock(html) { SourceTag = element.LocalName });
            }
        }

        private static void ReadText(IText text, ReaderContext ctx)
        {
            // AngleSharp already decodes entities into Data.
            var value = text.Data;

            // Drop whitespace-only text between block elements; keep it inside inline content
            // (including a block container with an open implicit paragraph still accruing inline).
            if (string.IsNullOrWhiteSpace(value) && !ctx.InInlineFlow)
            {
                return;
            }

            ctx.Emit(new MdText(value) { SourceTag = "#text" });
        }
    }
}
