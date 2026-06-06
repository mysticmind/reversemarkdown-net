using ReverseMarkdown.Dom;
using ReverseMarkdown.Helpers;

namespace ReverseMarkdown.Writers
{
    /// <summary>
    /// The library's default writer. Phase B uses the <see cref="MarkdownWriterBase"/>
    /// behavior; v5-default specifics (indented code, <c>* * *</c> rules, etc.) land as
    /// overrides here in a later phase. Byte parity with v5 is explicitly not a goal.
    /// </summary>
    public sealed class DefaultWriter : MarkdownWriterBase
    {
        public DefaultWriter(Config config) : base(config)
        {
        }
    }

    /// <summary>
    /// GitHub Flavored Markdown writer. GFM is CommonMark + extensions, so it inherits the
    /// CommonMark text handling (escaping, soft breaks, line-start escaping) and adds the GFM
    /// extensions (pipe tables, task lists, <c>~~</c> strikethrough, autolinks) from the base.
    /// </summary>
    public sealed class GithubWriter : CommonMarkWriter
    {
        public GithubWriter(Config config) : base(config)
        {
        }
    }

    /// <summary>
    /// Slack mrkdwn writer: single-char emphasis (<c>*</c>/<c>_</c>/<c>~</c>), bullet •,
    /// <c>&lt;url|text&gt;</c> links, bold "headings", and unsupported constructs (tables,
    /// images, rules, superscript) raise as in v5.
    /// </summary>
    public sealed class SlackWriter : MarkdownWriterBase
    {
        public SlackWriter(Config config) : base(config)
        {
        }

        protected override string StrongDelimiter => "*";
        protected override string EmphasisDelimiter => "_";
        protected override string StrikethroughDelimiter => "~";
        protected override string UnorderedBullet => "•";

        // Slack has no nested-emphasis alternation.
        protected override string StrongDelimiterAt(int depth) => "*";
        protected override string EmphasisDelimiterAt(int depth) => "_";

        // Slack mrkdwn does not use backslash escaping.
        protected override void AppendText(string text) => Buffer.Append(text);

        public override void Visit(MdHeading node) => Wrap("*", node.Children); // Slack has no headings

        public override void Visit(MdLink node)
        {
            var text = Capture(() => WriteInline(node.Children)).Trim();
            if (string.IsNullOrEmpty(text) || text == node.Url)
            {
                Buffer.Append('<').Append(node.Url).Append('>');
            }
            else
            {
                Buffer.Append('<').Append(node.Url).Append('|').Append(text).Append('>');
            }
        }

        public override void Visit(MdImage node) => throw new SlackUnsupportedTagException("img");

        public override void Visit(MdTable node) => throw new SlackUnsupportedTagException("table");

        public override void Visit(MdThematicBreak node) => throw new SlackUnsupportedTagException("hr");

        public override void Visit(MdSuperscript node) => throw new SlackUnsupportedTagException("sup");
    }

    /// <summary>
    /// Telegram MarkdownV2 writer: single-char emphasis, escaped special characters in text,
    /// escaped thematic break, and bold "headings".
    /// </summary>
    public sealed class TelegramWriter : MarkdownWriterBase
    {
        public TelegramWriter(Config config) : base(config)
        {
        }

        protected override string StrongDelimiter => "*";
        protected override string EmphasisDelimiter => "_";
        protected override string StrikethroughDelimiter => "~";

        protected override string StrongDelimiterAt(int depth) => "*";
        protected override string EmphasisDelimiterAt(int depth) => "_";

        protected override void AppendText(string text) =>
            Buffer.Append(StringUtils.EscapeTelegramMarkdownV2(text));

        public override void Visit(MdHeading node) => Wrap("*", node.Children); // Telegram has no headings

        public override void Visit(MdThematicBreak node) => Buffer.Append("\\-\\-\\-");
    }

    /// <summary>
    /// MultiMarkdown writer. Renders MMD-native constructs the base degrades — currently
    /// subscript (<c>~x~</c>). Footnotes/metadata/math/citations land with their readers in Phase E.
    /// </summary>
    public sealed class MultiMarkdownWriter : CommonMarkWriter
    {
        public MultiMarkdownWriter(Config config) : base(config)
        {
        }

        public override string Write(MarkdownDocument document)
        {
            base.Write(document); // leaves the rendered document in Buffer

            // MMD abbreviation definitions are appended at the document end.
            foreach (var abbreviation in document.Meta.Abbreviations)
            {
                Buffer.Append("\n\n*[").Append(abbreviation.Key).Append("]: ").Append(abbreviation.Value);
            }

            return Buffer.ToString();
        }

        public override void Visit(MdSubscript node) => Wrap("~", node.Children);

        // MultiMarkdown attaches list-item continuation blocks at a fixed 4-space tab stop,
        // not at the marker width the way CommonMark does.
        protected override int ContinuationIndent(int markerWidth) => 4;

        // MMD treats -, *, + as one list type, so adjacent lists need an explicit separator.
        protected override string? ListSeparatorComment => "<!-- -->";

        public override void Visit(MdCitation node)
        {
            if (!string.IsNullOrEmpty(node.Key))
            {
                Buffer.Append("[#").Append(node.Key).Append(']');
                return;
            }

            base.Visit(node);
        }

        protected override void WritePreamble(MarkdownDocument document)
        {
            if (document.Meta.Metadata.Count == 0)
            {
                return;
            }

            foreach (var pair in document.Meta.Metadata)
            {
                Buffer.Append(pair.Key).Append(": ").Append(pair.Value).Append('\n');
            }

            Buffer.Append('\n');
        }
    }

    /// <summary>
    /// Pandoc writer. Renders Pandoc-native constructs the base degrades — currently
    /// subscript (<c>~x~</c>). Fenced divs/bracketed spans/heading attrs land in Phase E.
    /// </summary>
    public sealed class PandocWriter : CommonMarkWriter
    {
        public PandocWriter(Config config) : base(config)
        {
        }

        public override void Visit(MdSubscript node) => Wrap("~", node.Children);

        // Pandoc treats -, *, + as one list type, so adjacent lists need an explicit separator.
        protected override string? ListSeparatorComment => "<!-- -->";

        // Pandoc folds a tight blockquote/code/heading into the preceding paragraph as a lazy
        // continuation, so a list item's continuation blocks need a blank line before them.
        protected override bool ForceBlankLineBeforeItemBlock => true;

        // Pandoc miscounts the indent of a code block opening on the list-marker line; emit the
        // marker alone and indent the block on the next line.
        protected override bool MarkerOnOwnLineForLeadingBlock => true;

        // Pandoc parses a symmetric "***foo***" as strong-outer, unlike CommonMark (em-outer). Only
        // the sole-child nesting produces that symmetric run, so disambiguate exactly there: an
        // emphasis directly wrapping just a strong (or vice versa) mixes families — asterisk for the
        // emphasis (works intraword, e.g. foo*__bar__*baz) and underscore for the inner strong — so
        // the delimiters never merge. Other arrangements keep the base per-type alternation.
        public override void Visit(MdEmphasis node)
        {
            if (node.Children.Count == 1 && node.Children[0] is MdStrong strong)
            {
                Buffer.Append('*');
                Wrap("__", strong.Children);
                Buffer.Append('*');
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdStrong node)
        {
            if (node.Children.Count == 1 && node.Children[0] is MdEmphasis emphasis)
            {
                Buffer.Append("**");
                Wrap("_", emphasis.Children);
                Buffer.Append("**");
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdCitation node)
        {
            if (!string.IsNullOrEmpty(node.Key))
            {
                Buffer.Append("[@").Append(node.Key).Append(']');
                return;
            }

            base.Visit(node);
        }

        public override void Visit(MdMath node)
        {
            var delimiter = node.Display ? "$$" : "$";
            Buffer.Append(delimiter).Append(node.Literal).Append(delimiter);
        }

        public override void Visit(MdHeading node)
        {
            base.Visit(node);
            if (node.Attributes is not null)
            {
                Buffer.Append(' ').Append(FormatAttributes(node.Attributes));
            }
        }

        public override void Visit(MdFencedDiv node)
        {
            Buffer.Append("::: ").Append(FormatAttributes(node.Attributes)).Append('\n');
            WriteBlocks(node.Children);
            Buffer.Append("\n:::");
        }

        public override void Visit(MdBracketedSpan node)
        {
            Buffer.Append('[');
            WriteInline(node.Children);
            Buffer.Append(']').Append(FormatAttributes(node.Attributes));
        }

        public override void Visit(MdLineBlock node)
        {
            var inner = Capture(() => WriteInline(node.Children)).Replace("\r\n", "\n");
            var lines = inner.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    Buffer.Append('\n');
                }

                Buffer.Append("| ").Append(lines[i].TrimEnd());
            }
        }

        protected override void WritePreamble(MarkdownDocument document)
        {
            if (document.Meta.Metadata.Count == 0)
            {
                return;
            }

            Buffer.Append("---\n");
            foreach (var pair in document.Meta.Metadata)
            {
                Buffer.Append(pair.Key).Append(": ").Append(pair.Value).Append('\n');
            }

            Buffer.Append("---\n\n");
        }
    }

    /// <summary>
    /// Maps a <see cref="Config.MarkdownFlavor"/> to a writer. Flavors without a dedicated
    /// writer yet fall back to <see cref="DefaultWriter"/> (tracked in docs/v6/migration.md).
    /// </summary>
    public static class WriterFactory
    {
        public static IMarkdownWriter Create(Config.MarkdownFlavor flavor, Config config)
        {
            return flavor switch
            {
                Config.MarkdownFlavor.GitHub => new GithubWriter(config),
                Config.MarkdownFlavor.CommonMark => new CommonMarkWriter(config),
                Config.MarkdownFlavor.Slack => new SlackWriter(config),
                Config.MarkdownFlavor.Telegram => new TelegramWriter(config),
                Config.MarkdownFlavor.MultiMarkdown => new MultiMarkdownWriter(config),
                Config.MarkdownFlavor.Pandoc => new PandocWriter(config),
                _ => new DefaultWriter(config),
            };
        }
    }
}
