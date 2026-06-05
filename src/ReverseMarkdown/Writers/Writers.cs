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
    /// GitHub Flavored Markdown writer. The base already produces GFM-compatible output
    /// (fenced code, pipe tables, <c>~~</c> strikethrough, task lists), so this is currently a
    /// thin specialization — the clearest demonstration that GFM ≈ the base flavor.
    /// </summary>
    public sealed class GithubWriter : MarkdownWriterBase
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

        protected override void AppendText(string text) =>
            Buffer.Append(StringUtils.EscapeTelegramMarkdownV2(text));

        public override void Visit(MdHeading node) => Wrap("*", node.Children); // Telegram has no headings

        public override void Visit(MdThematicBreak node) => Buffer.Append("\\-\\-\\-");
    }

    /// <summary>
    /// MultiMarkdown writer. Renders MMD-native constructs the base degrades — currently
    /// subscript (<c>~x~</c>). Footnotes/metadata/math/citations land with their readers in Phase E.
    /// </summary>
    public sealed class MultiMarkdownWriter : MarkdownWriterBase
    {
        public MultiMarkdownWriter(Config config) : base(config)
        {
        }

        public override void Visit(MdSubscript node) => Wrap("~", node.Children);

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
    public sealed class PandocWriter : MarkdownWriterBase
    {
        public PandocWriter(Config config) : base(config)
        {
        }

        public override void Visit(MdSubscript node) => Wrap("~", node.Children);

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
