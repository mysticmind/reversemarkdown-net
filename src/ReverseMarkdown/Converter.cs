using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using ReverseMarkdown.Dom;
using ReverseMarkdown.Readers;
using ReverseMarkdown.Writers;


namespace ReverseMarkdown {
    /// <summary>
    /// Converts HTML to Markdown. Thread-safe for concurrent use.
    /// </summary>
    public class Converter {
        // AngleSharp parser and Markdown DOM reader are reusable across parses.
        private readonly HtmlParser _htmlParser = new();
        private readonly MarkdownDomReader _markdownDomReader;

        public Converter() : this(new Config())
        {
        }

        public Converter(Config config) : this(config, null)
        {
        }

        public Converter(Config config, params Assembly[]? additionalAssemblies)
        {
            // The legacy CommonMark switch is equivalent to the CommonMark flavor (roundtrip-faithful
            // reader + writer). Normalize it up front so the reader and writer agree on the flavor.
            if (config.CommonMark && config.Flavor == Config.MarkdownFlavor.Default)
            {
                config.Flavor = Config.MarkdownFlavor.CommonMark;
            }

            Config = config;
            _markdownDomReader = new MarkdownDomReader(Config, additionalAssemblies);
        }

        public Config Config { get; protected set; }

        /// <summary>
        /// The flavor actually used to render. The legacy boolean switches
        /// (<see cref="Config.SlackFlavored"/>, <see cref="Config.TelegramMarkdownV2"/>) select
        /// their dedicated writer; otherwise <see cref="Config.Flavor"/> applies.
        /// </summary>
        internal Config.MarkdownFlavor EffectiveFlavor =>
            Config.SlackFlavored ? Config.MarkdownFlavor.Slack
            : Config.TelegramMarkdownV2 ? Config.MarkdownFlavor.Telegram
            : Config.Flavor;

        public virtual string Convert(string html)
        {
            var flavor = EffectiveFlavor;
            if (flavor == Config.MarkdownFlavor.Slack)
            {
                // Slack has no tables. A well-formed <table> raises in the writer (MdTable), but the
                // HTML5 parser silently discards orphan <td>/<tr>/<th> before the reader sees them,
                // so guard those tags here to keep v5's "unsupported tag" behavior.
                var orphan = OrphanTablePart.Match(html);
                if (orphan.Success)
                {
                    throw new SlackUnsupportedTagException(orphan.Groups[1].Value.ToLowerInvariant());
                }
            }

            if (Config.IsCommonMarkBased(flavor))
            {
                // CommonMark passes block-level / leading-close-tag / comment HTML through verbatim.
                var normalized = html.ReplaceLineEndings("\n");
                if (LooksLikeCommonMarkHtmlBlock(normalized))
                {
                    return ApplyOutputLineEndings(normalized);
                }

                var trimmed = normalized.TrimStart('﻿', ' ', '\t', '\r', '\n');
                if (trimmed.StartsWith("</", StringComparison.Ordinal) ||
                    normalized.Contains("<!--", StringComparison.Ordinal) ||
                    normalized.Contains("<![CDATA[", StringComparison.Ordinal))
                {
                    return ApplyOutputLineEndings(normalized);
                }
            }

            return Render(Parse(html, collectMetadata: EmitsMetadata(flavor)), flavor);
        }

        /// <summary>
        /// Parse HTML into a mutable <see cref="MarkdownDocument"/> you can traverse, filter and
        /// reshape before rendering. Uses AngleSharp's HTML5-compliant parser.
        /// </summary>
        public virtual MarkdownDocument Parse(string html)
        {
            return Parse(html, collectMetadata: true);
        }

        private MarkdownDocument Parse(string html, bool collectMetadata)
        {
            html = html.ReplaceLineEndings("\n");
            html = Cleaner.FixUnclosedScriptStyle(html);

            // Trailing whitespace after the last block is insignificant, but the HTML5 parser will
            // reconstruct an unclosed formatting element (e.g. "<p>x <a></p>\n") around it, emitting
            // a phantom trailing element. Trim it so that does not leak into the output.
            html = html.TrimEnd();

            var document = _htmlParser.ParseDocument(html);
            var body = document.Body!;
            ApplyHtmlFilters(body);
            var markdownDocument = _markdownDomReader.Read(body);
            if (collectMetadata)
            {
                CollectMetadata(document, markdownDocument);
            }

            return markdownDocument;
        }

        private static bool EmitsMetadata(Config.MarkdownFlavor flavor) =>
            flavor is Config.MarkdownFlavor.MultiMarkdown or Config.MarkdownFlavor.Pandoc;

        // Collect document metadata (title + <meta name=.. content=..>) for MMD/Pandoc.
        // Flavor-agnostic: always collected; the writer decides whether to emit it.
        private static void CollectMetadata(AngleSharp.Dom.IDocument document, MarkdownDocument markdownDocument)
        {
            var title = document.Title;
            if (!string.IsNullOrWhiteSpace(title))
            {
                markdownDocument.Meta.Metadata.Add(new("title", title.Trim()));
            }

            var head = document.Head;
            if (head is null)
            {
                return;
            }

            foreach (var meta in head.QuerySelectorAll("meta[name]"))
            {
                var name = meta.GetAttribute("name");
                var content = meta.GetAttribute("content");
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
                {
                    markdownDocument.Meta.Metadata.Add(new(name, content));
                }
            }
        }

        // Remove HTML elements matched by the configured CSS selectors / predicate filters
        // before reading (HTML-side filtering for issue #79).
        private void ApplyHtmlFilters(AngleSharp.Dom.IElement root)
        {
            foreach (var selector in Config.HtmlExcludeSelectors)
            {
                if (string.IsNullOrWhiteSpace(selector))
                {
                    continue;
                }

                foreach (var element in root.QuerySelectorAll(selector).ToList())
                {
                    element.Remove();
                }
            }

            if (Config.HtmlElementFilters.Count > 0)
            {
                var toRemove = root.QuerySelectorAll("*")
                    .Where(e => Config.HtmlElementFilters.Any(f => f(e)))
                    .ToList();
                foreach (var element in toRemove)
                {
                    element.Remove();
                }
            }
        }

        /// <summary>
        /// Render a <see cref="MarkdownDocument"/> to markdown using the writer selected by
        /// <see cref="Config.Flavor"/>.
        /// </summary>
        public virtual string Render(MarkdownDocument document) => Render(document, EffectiveFlavor);

        /// <summary>
        /// Render a <see cref="MarkdownDocument"/> to markdown in the given flavor.
        /// </summary>
        public virtual string Render(MarkdownDocument document, Config.MarkdownFlavor flavor)
        {
            var writer = WriterFactory.Create(flavor, Config);
            // Trim leading/trailing blank lines (e.g. from an empty <p></p> at the document edge).
            return ApplyOutputLineEndings(writer.Write(document).Trim('\n', '\r'));
        }

        private string ApplyOutputLineEndings(string content)
        {
            var lineEnding = string.IsNullOrEmpty(Config.OutputLineEnding)
                ? Environment.NewLine
                : Config.OutputLineEnding;
            return content.ReplaceLineEndings(lineEnding);
        }

        private static bool LooksLikeCommonMarkHtmlBlock(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) {
                return false;
            }

            var trimmed = html.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
            if (trimmed.StartsWith("<!--", StringComparison.Ordinal) ||
                trimmed.StartsWith("<?", StringComparison.Ordinal) ||
                trimmed.StartsWith("<!", StringComparison.Ordinal)) {
                return true;
            }

            return HtmlBlockStart.IsMatch(trimmed);
        }

        private static readonly Regex OrphanTablePart = new(
            @"<(td|tr|th)(\s|/|>)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly Regex HtmlBlockStart = new(
            @"^\s*<\/?(div|table|pre|script|style|iframe|article|section|header|footer|nav|aside|blockquote|h[1-6]|hr|details|summary|figure|figcaption|main|form|center|address|body|html|head|link|meta|title|tbody|thead|tfoot|tr|td|th)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

    }
}
