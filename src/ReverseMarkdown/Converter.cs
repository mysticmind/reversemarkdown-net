using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AngleSharp;
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
        private readonly HtmlParser _htmlParser;
        private readonly MarkdownDomReader _markdownDomReader;

        public Converter() : this(new Config())
        {
        }

        // Trimming/AOT-safe: uses the built-in readers only. Add custom readers with RegisterReader.
        public Converter(Config config)
        {
            Config = config;
            _htmlParser = new HtmlParser();
            _markdownDomReader = new MarkdownDomReader(config);
        }

        /// <summary>
        /// Creates a converter that parses HTML in the given AngleSharp browsing context, so
        /// preprocessing steps can use AngleSharp extensions the library does not depend on. The
        /// canonical case is <c>AngleSharp.Css</c>, which adds the CSS cascade and makes
        /// <c>ComputeCurrentStyle()</c> usable inside a custom step:
        /// <code>
        /// var context = BrowsingContext.New(Configuration.Default.WithCss());
        /// var config = new Config();
        /// config.Preprocess.RemoveWhere(e =&gt; e.ComputeCurrentStyle().GetPropertyValue("display") == "none");
        /// var converter = new Converter(config, context);
        /// </code>
        /// </summary>
        /// <remarks>
        /// The context is used for every parse. Two caveats: AngleSharp fetches external resources
        /// if your configuration registers a requester, and a shared browsing context is not
        /// guaranteed safe for concurrent parsing, so give each thread its own converter and context
        /// if you convert in parallel.
        /// </remarks>
        public Converter(Config config, IBrowsingContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Config = config;
            _htmlParser = new HtmlParser(new HtmlParserOptions(), context);
            _markdownDomReader = new MarkdownDomReader(config);
        }

        [RequiresUnreferencedCode("Scans the given assemblies for [MarkdownReader] reader types via reflection; those types may be removed by the trimmer. Use RegisterReader to add custom readers under trimming/AOT.")]
        [RequiresDynamicCode("Instantiates discovered reader types via Activator.CreateInstance.")]
        public Converter(Config config, params Assembly[]? additionalAssemblies)
        {
            Config = config;
            _htmlParser = new HtmlParser();
            _markdownDomReader = additionalAssemblies is { Length: > 0 }
                ? new MarkdownDomReader(config, additionalAssemblies)
                : new MarkdownDomReader(config);
        }

        public Config Config { get; protected set; }

        /// <summary>
        /// Registers a custom <see cref="IMdReader"/> for a tag, overriding any built-in reader for
        /// that tag. This is the trimming/Native-AOT-safe way to add custom readers (no assembly
        /// scanning). Call before converting; not safe to call concurrently with conversion.
        /// </summary>
        public void RegisterReader(string tag, IMdReader reader) => _markdownDomReader.Register(tag, reader);

        /// <summary>The flavor used to render. The legacy boolean switches map into
        /// <see cref="Config.Flavor"/>, so it is the single source of truth.</summary>
        internal Config.MarkdownFlavor EffectiveFlavor => Config.Flavor;

        public virtual string Convert(string html)
        {
            var flavor = EffectiveFlavor;

            // Slack inspects the raw markup for orphan table parts and the CommonMark-based flavors
            // pass raw HTML blocks straight through, so for those the preprocessing steps have to be
            // materialized back into HTML before those checks run. Every other flavor applies them
            // in-DOM during Parse, without the extra round-trip.
            var preprocessed = false;
            if (Config.Preprocess.HasSteps &&
                (flavor == Config.MarkdownFlavor.Slack || Config.IsCommonMarkBased(flavor)))
            {
                html = Preprocess(html);
                preprocessed = true;
            }

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

            return Render(Parse(html, collectMetadata: EmitsMetadata(flavor), alreadyPreprocessed: preprocessed), flavor);
        }

        /// <summary>
        /// Applies the configured <see cref="Config.Preprocess"/> steps to <paramref name="html"/>
        /// and returns the transformed HTML. <see cref="Convert"/> and <see cref="Parse(string)"/>
        /// do this for you; call it directly to inspect, cache or further process the preprocessed
        /// markup. Returns the input unchanged when no steps are configured.
        /// </summary>
        public virtual string Preprocess(string html)
        {
            if (!Config.Preprocess.HasSteps)
            {
                return html;
            }

            var normalized = Config.Preprocess.ApplyText(html.ReplaceLineEndings("\n"));
            var document = _htmlParser.ParseDocument(Cleaner.FixUnclosedScriptStyle(normalized));
            Config.Preprocess.Apply(document.DocumentElement!);
            EnsureDocumentStructure(document);

            // Give back markup shaped like the input: a fragment stays a fragment so that the
            // flavor-level raw-HTML checks in Convert still see what the caller passed in. Only when
            // the parser produced a real <head> (title/meta, which the metadata collector needs) is
            // the full document serialized.
            return document.Head is { HasChildNodes: true }
                ? document.DocumentElement!.OuterHtml
                : document.Body!.InnerHtml;
        }

        /// <summary>
        /// Parse HTML into a mutable <see cref="MarkdownDocument"/> you can traverse, filter and
        /// reshape before rendering. Uses AngleSharp's HTML5-compliant parser. Any configured
        /// <see cref="Config.Preprocess"/> steps run first.
        /// </summary>
        public virtual MarkdownDocument Parse(string html)
        {
            return Parse(html, collectMetadata: true, alreadyPreprocessed: false);
        }

        private MarkdownDocument Parse(string html, bool collectMetadata, bool alreadyPreprocessed)
        {
            html = html.ReplaceLineEndings("\n");
            if (!alreadyPreprocessed)
            {
                // Text steps see normalized line endings, and run before the parser can discard or
                // reinterpret markup they might need to repair.
                html = Config.Preprocess.ApplyText(html);
            }

            html = Cleaner.FixUnclosedScriptStyle(html);

            // Trailing whitespace after the last block is insignificant, but the HTML5 parser will
            // reconstruct an unclosed formatting element (e.g. "<p>x <a></p>\n") around it, emitting
            // a phantom trailing element. Trim it so that does not leak into the output.
            html = html.TrimEnd();

            var document = _htmlParser.ParseDocument(html);
            if (!alreadyPreprocessed)
            {
                // Runs against the document element so steps can reach both <head> (hoisted styles,
                // scripts, metadata) and <body>.
                Config.Preprocess.Apply(document.DocumentElement!);
                EnsureDocumentStructure(document);
            }

            var body = document.Body!;
            ApplyHtmlFilters(body);
            var markdownDocument = _markdownDomReader.Read(body);
            if (collectMetadata)
            {
                CollectMetadata(document, markdownDocument);
            }

            return markdownDocument;
        }

        // The built-in steps never detach <html>/<head>/<body>, but a custom step can. Fail with an
        // explanation rather than the NullReferenceException the reader would otherwise throw.
        private static void EnsureDocumentStructure(AngleSharp.Dom.IDocument document)
        {
            if (document.DocumentElement is null || document.Body is null)
            {
                throw new InvalidOperationException(
                    "A preprocessing step removed the document structure. Steps must leave the " +
                    "<html>, <head> and <body> elements in place; remove their contents instead.");
            }
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
            foreach (var selector in Config.Html.ExcludeSelectors)
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

            if (Config.Html.ElementFilters.Count > 0)
            {
                var toRemove = root.QuerySelectorAll("*")
                    .Where(e => Config.Html.ElementFilters.Any(f => f(e)))
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
            var lineEnding = string.IsNullOrEmpty(Config.Formatting.OutputLineEnding)
                ? Environment.NewLine
                : Config.Formatting.OutputLineEnding;
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
