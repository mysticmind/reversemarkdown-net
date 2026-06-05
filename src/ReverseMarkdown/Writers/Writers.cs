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
    /// GitHub Flavored Markdown writer. Differences from the base (task lists, tables,
    /// fenced code, strikethrough) land as overrides here as those nodes are ported.
    /// </summary>
    public sealed class GithubWriter : MarkdownWriterBase
    {
        public GithubWriter(Config config) : base(config)
        {
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
                _ => new DefaultWriter(config),
            };
        }
    }
}
