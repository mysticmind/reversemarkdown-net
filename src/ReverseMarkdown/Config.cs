using System;
using System.Collections.Generic;
using AngleSharp.Dom;

namespace ReverseMarkdown
{
    /// <summary>
    /// Conversion configuration. Options are organized into groups:
    /// <see cref="Flavor"/> (the Markdown flavor), <see cref="Images"/>, <see cref="Links"/>,
    /// <see cref="Tables"/>, <see cref="Tags"/>, <see cref="Html"/> and <see cref="Formatting"/>.
    /// The former flat properties are still available but obsolete — they forward to the grouped
    /// members and will be removed in a future major version.
    /// </summary>
    public class Config
    {
        // ---- Flavor (canonical selector) ----

        /// <summary>
        /// The Markdown flavor to produce. This is the single, canonical flavor selector; the legacy
        /// <c>GithubFlavored</c>/<c>SlackFlavored</c>/<c>TelegramMarkdownV2</c>/<c>CommonMark</c>
        /// switches are obsolete aliases that read and write this property.
        /// </summary>
        public MarkdownFlavor Flavor { get; set; } = MarkdownFlavor.Default;

        public enum MarkdownFlavor
        {
            Default,
            GitHub,
            CommonMark,
            Slack,
            Telegram,
            MultiMarkdown,
            Pandoc
        }

        /// <summary>When CommonMark is selected, insert spaces to avoid intraword emphasis
        /// (<c>he&lt;strong&gt;ll&lt;/strong&gt;o</c> becomes <c>he **ll** o</c>). Default is false.</summary>
        public bool CommonMarkIntrawordEmphasisSpacing { get; set; } = false;

        /// <summary>When CommonMark is selected, emit HTML for inline tags (em/strong/a/img) to
        /// avoid delimiter edge cases. Default is true.</summary>
        public bool CommonMarkUseHtmlInlineTags { get; set; } = true;

        /// <summary>
        /// Enables GitHub Flavored Markdown conversion on the default writer: br, pre → fenced code,
        /// and task lists. Tables are always emitted as GFM regardless of this flag. This is
        /// <b>not</b> the same as <see cref="MarkdownFlavor.GitHub"/>: that flavor selects the
        /// CommonMark-based GitHub writer (which preserves raw HTML), whereas this switch produces
        /// clean GFM markdown on the default conversion path. Default is false.
        /// </summary>
        public bool GithubFlavored { get; set; } = false;

        // ---- Grouped options ----

        /// <summary>Image handling options (base64 images, lazy-loading fallback).</summary>
        public ImageOptions Images { get; } = new();

        /// <summary>Link handling options (smart href, URI scheme whitelist).</summary>
        public LinkOptions Links { get; } = new();

        /// <summary>Table handling options.</summary>
        public TableOptions Tables { get; } = new();

        /// <summary>Tag handling options (unknown-tag strategy, replacers, aliases, pass-through).</summary>
        public TagOptions Tags { get; } = new();

        /// <summary>HTML pre-filtering options (v6 Markdown DOM path).</summary>
        public HtmlFilterOptions Html { get; } = new();

        /// <summary>Output formatting options (whitespace, line endings, bullets, code blocks).</summary>
        public FormattingOptions Formatting { get; } = new();

        // ---- Flavor helpers (internal) ----

        /// <summary>Flavors built on CommonMark that share its text handling (escaping, soft
        /// breaks) and inline/block raw-HTML passthrough. GFM and MultiMarkdown qualify (both
        /// preserve raw HTML); Pandoc does not, since it renders div/span as fenced divs/spans.</summary>
        internal static bool IsCommonMarkBased(MarkdownFlavor flavor) =>
            flavor is MarkdownFlavor.CommonMark or MarkdownFlavor.GitHub;

        /// <summary>Flavors that preserve inline raw HTML (and HTML comments) verbatim rather than
        /// converting tags they lack clean markdown for. CommonMark/GFM do; MultiMarkdown also does
        /// (it passes <c>&lt;del&gt;</c>, <c>&lt;i class&gt;</c>, etc. through and has no <c>~~</c>
        /// strikethrough). Pandoc is excluded — it has native spans/strikeout markup.</summary>
        internal static bool PreservesInlineRawHtml(MarkdownFlavor flavor) =>
            IsCommonMarkBased(flavor) || flavor is MarkdownFlavor.MultiMarkdown;

        /// <summary>Determines whether a URI scheme is allowed for &lt;a&gt;/&lt;img&gt;: the
        /// whitelist is empty (allow all) or contains the scheme.</summary>
        internal bool IsSchemeWhitelisted(string scheme)
        {
            if (scheme == null) throw new ArgumentNullException(nameof(scheme));
            return Links.WhitelistedSchemes.Count == 0 || Links.WhitelistedSchemes.Contains(scheme);
        }

        // ---- Grouped option types ----

        /// <summary>Image handling options.</summary>
        public sealed class ImageOptions
        {
            /// <summary>How base64-encoded images (inline data URIs) are handled. Default is
            /// <see cref="Base64ImageHandling.Include"/>.</summary>
            public Base64ImageHandling Base64Handling { get; set; } = Base64ImageHandling.Include;

            /// <summary>When <see cref="Base64Handling"/> is <see cref="Base64ImageHandling.SaveToFile"/>,
            /// the directory to save images to.</summary>
            public string? Base64Directory { get; set; }

            /// <summary>When saving base64 images to file, generates a filename (without extension)
            /// from the image index and MIME type. Defaults to <c>image_0</c>, <c>image_1</c>, ….</summary>
            public Func<int, string, string>? Base64FileName { get; set; }

            /// <summary>When enabled, an &lt;img&gt; whose <c>src</c> is empty or a <c>data:</c>
            /// placeholder (as used by lazy-loading libraries) falls back to the first usable URL in
            /// <see cref="LazySourceAttributes"/>. Default is false.</summary>
            public bool LazySrcFallback { get; set; } = false;

            /// <summary>Ordered list of attributes consulted (first usable wins) when
            /// <see cref="LazySrcFallback"/> is enabled and <c>src</c> is empty or a placeholder.</summary>
            public List<string> LazySourceAttributes { get; set; } =
                ["data-src", "data-original", "data-lazy-src", "data-srcset", "data-original-src"];
        }

        /// <summary>Link handling options.</summary>
        public sealed class LinkOptions
        {
            /// <summary>How to handle an &lt;a&gt; href. When true, if the link text and href are the
            /// same the plain text is emitted (with http/https and tel:/mailto: refinements). Default
            /// is false.</summary>
            public bool SmartHref { get; set; } = false;

            /// <summary>Allowed URI schemes (without trailing colon) for &lt;a&gt;/&lt;img&gt;. Empty
            /// (default) allows everything; others are bypassed.</summary>
            public HashSet<string> WhitelistedSchemes { get; } = new(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Table handling options.</summary>
        public sealed class TableOptions
        {
            /// <summary>How a table without a header row is handled. Default is
            /// <see cref="TableWithoutHeaderRowHandlingOption.Default"/> (first row becomes header).</summary>
            public TableWithoutHeaderRowHandlingOption WithoutHeaderRow { get; set; } =
                TableWithoutHeaderRowHandlingOption.Default;

            /// <summary>Handle table header columns that use column spans. Default is true.</summary>
            public bool HeaderColumnSpans { get; set; } = true;
        }

        /// <summary>Tag handling options.</summary>
        public sealed class TagOptions
        {
            /// <summary>How unknown (unsupported) tags are handled. Default is
            /// <see cref="UnknownTagsOption.PassThrough"/>.</summary>
            public UnknownTagsOption Unknown { get; set; } = UnknownTagsOption.PassThrough;

            /// <summary>Optional markdown wrappers for unknown tags. Key is the tag name; value is the
            /// wrapper emitted as both prefix and suffix around converted content.</summary>
            public Dictionary<string, string> Replacer { get; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>Optional alias map to treat a tag as another tag during conversion.</summary>
            public Dictionary<string, string> Aliases { get; } = new(StringComparer.OrdinalIgnoreCase);

            /// <summary>Tags to pass through as-is without any processing.</summary>
            public HashSet<string> PassThrough { get; set; } = [];
        }

        /// <summary>HTML pre-filtering options (v6 Markdown DOM path).</summary>
        public sealed class HtmlFilterOptions
        {
            /// <summary>EXPERIMENTAL (v6): CSS selectors whose matching elements are removed from the
            /// HTML before conversion via the Markdown DOM path (<see cref="Converter.Parse"/>).</summary>
            public HashSet<string> ExcludeSelectors { get; } = new();

            /// <summary>EXPERIMENTAL (v6): predicate filters run against every element before
            /// conversion; an element for which any predicate returns true is removed.</summary>
            public List<Func<IElement, bool>> ElementFilters { get; } = new();
        }

        /// <summary>Output formatting options.</summary>
        public sealed class FormattingOptions
        {
            /// <summary>Clean up unnecessary spaces in the output. Default is true.</summary>
            public bool CleanupSpaces { get; set; } = true;

            /// <summary>Remove the leading newlines a <c>div</c> would otherwise introduce. Default is false.</summary>
            public bool SuppressDivNewlines { get; set; } = false;

            /// <summary>Remove HTML comments (and their text) from the output. Default is false.</summary>
            public bool RemoveComments { get; set; } = false;

            /// <summary>Treat &lt;pre&gt; (and &lt;pre&gt;&lt;code&gt;) content as normal HTML instead
            /// of a code block. Default is false.</summary>
            public bool PreAsHtml { get; set; } = false;

            /// <summary>Escape markdown line starts (headings, lists, block markers) in plain text
            /// output. Default is false.</summary>
            public bool EscapeLineStarts { get; set; } = false;

            /// <summary>Output line endings for the generated markdown. Defaults to
            /// <see cref="Environment.NewLine"/>.</summary>
            public string OutputLineEnding { get; set; } = Environment.NewLine;

            /// <summary>Bullet character for unordered lists. Default is <c>-</c>.</summary>
            public char ListBulletChar { get; set; } = '-';

            /// <summary>Default GFM code block language when class-based language markers are absent.</summary>
            public string? DefaultCodeBlockLanguage { get; set; }
        }

        // ---- Option enums (kept nested for backward compatibility) ----

        public enum UnknownTagsOption
        {
            /// <summary>Include the unknown tag completely into the result (tag plus text). Default.</summary>
            PassThrough,

            /// <summary>Drop the unknown tag and its content.</summary>
            Drop,

            /// <summary>Ignore the unknown tag but try to convert its content.</summary>
            Bypass,

            /// <summary>Raise an error to let you know.</summary>
            Raise
        }

        public enum TableWithoutHeaderRowHandlingOption
        {
            /// <summary>By default, the first row is used as the header row.</summary>
            Default,

            /// <summary>An empty row is added as the header row.</summary>
            EmptyRow
        }

        public enum Base64ImageHandling
        {
            /// <summary>Include base64-encoded images in the markdown output (default).</summary>
            Include,

            /// <summary>Skip/ignore base64-encoded images entirely.</summary>
            Skip,

            /// <summary>Save base64-encoded images to disk and reference the saved file path.
            /// Requires <see cref="ImageOptions.Base64Directory"/> to be set.</summary>
            SaveToFile
        }

        // ---- Obsolete flat members (forward to the grouped members / Flavor) ----

        /// <summary>Legacy Slack-flavored switch. Alias for <c>Flavor = MarkdownFlavor.Slack</c>.</summary>
        [Obsolete("Use Flavor = Config.MarkdownFlavor.Slack.")]
        public bool SlackFlavored
        {
            get => Flavor == MarkdownFlavor.Slack;
            set => SetFlavorFlag(value, MarkdownFlavor.Slack);
        }

        /// <summary>Legacy Telegram MarkdownV2 switch. Alias for <c>Flavor = MarkdownFlavor.Telegram</c>.</summary>
        [Obsolete("Use Flavor = Config.MarkdownFlavor.Telegram.")]
        public bool TelegramMarkdownV2
        {
            get => Flavor == MarkdownFlavor.Telegram;
            set => SetFlavorFlag(value, MarkdownFlavor.Telegram);
        }

        /// <summary>Legacy CommonMark switch. Alias for <c>Flavor = MarkdownFlavor.CommonMark</c>.</summary>
        [Obsolete("Use Flavor = Config.MarkdownFlavor.CommonMark.")]
        public bool CommonMark
        {
            get => Flavor == MarkdownFlavor.CommonMark;
            set => SetFlavorFlag(value, MarkdownFlavor.CommonMark);
        }

        private void SetFlavorFlag(bool value, MarkdownFlavor flavor)
        {
            if (value)
            {
                Flavor = flavor;
            }
            else if (Flavor == flavor)
            {
                Flavor = MarkdownFlavor.Default;
            }
        }

        [Obsolete("Use Tags.Unknown.")]
        public UnknownTagsOption UnknownTags { get => Tags.Unknown; set => Tags.Unknown = value; }

        [Obsolete("Use Tags.Replacer.")]
        public Dictionary<string, string> UnknownTagsReplacer => Tags.Replacer;

        [Obsolete("Use Tags.Aliases.")]
        public Dictionary<string, string> TagAliases => Tags.Aliases;

        [Obsolete("Use Tags.PassThrough.")]
        public HashSet<string> PassThroughTags { get => Tags.PassThrough; set => Tags.PassThrough = value; }

        [Obsolete("Use Links.SmartHref.")]
        public bool SmartHrefHandling { get => Links.SmartHref; set => Links.SmartHref = value; }

        [Obsolete("Use Links.WhitelistedSchemes.")]
        public HashSet<string> WhitelistUriSchemes => Links.WhitelistedSchemes;

        [Obsolete("Use Tables.WithoutHeaderRow.")]
        public TableWithoutHeaderRowHandlingOption TableWithoutHeaderRowHandling
        {
            get => Tables.WithoutHeaderRow;
            set => Tables.WithoutHeaderRow = value;
        }

        [Obsolete("Use Tables.HeaderColumnSpans.")]
        public bool TableHeaderColumnSpanHandling { get => Tables.HeaderColumnSpans; set => Tables.HeaderColumnSpans = value; }

        [Obsolete("Use Images.Base64Handling.")]
        public Base64ImageHandling Base64Images { get => Images.Base64Handling; set => Images.Base64Handling = value; }

        [Obsolete("Use Images.Base64Directory.")]
        public string? Base64ImageSaveDirectory { get => Images.Base64Directory; set => Images.Base64Directory = value; }

        [Obsolete("Use Images.Base64FileName.")]
        public Func<int, string, string>? Base64ImageFileNameGenerator
        {
            get => Images.Base64FileName;
            set => Images.Base64FileName = value;
        }

        [Obsolete("Use Images.LazySrcFallback.")]
        public bool LazyImageSrcFallback { get => Images.LazySrcFallback; set => Images.LazySrcFallback = value; }

        [Obsolete("Use Images.LazySourceAttributes.")]
        public List<string> LazyImageSourceAttributes
        {
            get => Images.LazySourceAttributes;
            set => Images.LazySourceAttributes = value;
        }

        [Obsolete("Use Html.ExcludeSelectors.")]
        public HashSet<string> HtmlExcludeSelectors => Html.ExcludeSelectors;

        [Obsolete("Use Html.ElementFilters.")]
        public List<Func<IElement, bool>> HtmlElementFilters => Html.ElementFilters;

        [Obsolete("Use Formatting.CleanupSpaces.")]
        public bool CleanupUnnecessarySpaces { get => Formatting.CleanupSpaces; set => Formatting.CleanupSpaces = value; }

        [Obsolete("Use Formatting.SuppressDivNewlines.")]
        public bool SuppressDivNewlines { get => Formatting.SuppressDivNewlines; set => Formatting.SuppressDivNewlines = value; }

        [Obsolete("Use Formatting.RemoveComments.")]
        public bool RemoveComments { get => Formatting.RemoveComments; set => Formatting.RemoveComments = value; }

        [Obsolete("Use Formatting.PreAsHtml.")]
        public bool ConvertPreContentAsHtml { get => Formatting.PreAsHtml; set => Formatting.PreAsHtml = value; }

        [Obsolete("Use Formatting.EscapeLineStarts.")]
        public bool EscapeMarkdownLineStarts { get => Formatting.EscapeLineStarts; set => Formatting.EscapeLineStarts = value; }

        [Obsolete("Use Formatting.OutputLineEnding.")]
        public string OutputLineEnding { get => Formatting.OutputLineEnding; set => Formatting.OutputLineEnding = value; }

        [Obsolete("Use Formatting.DefaultCodeBlockLanguage.")]
        public string? DefaultCodeBlockLanguage
        {
            get => Formatting.DefaultCodeBlockLanguage;
            set => Formatting.DefaultCodeBlockLanguage = value;
        }

        /// <summary>Bullet character for unordered lists. Alias for <c>Formatting.ListBulletChar</c>
        /// (Slack always uses <c>•</c>).</summary>
        [Obsolete("Use Formatting.ListBulletChar.")]
        public char ListBulletChar
        {
            get => Flavor == MarkdownFlavor.Slack ? '•' : Formatting.ListBulletChar;
            set => Formatting.ListBulletChar = value;
        }
    }
}
