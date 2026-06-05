namespace ReverseMarkdown.Writers
{
    /// <summary>
    /// CommonMark writer. Phase A uses the <see cref="MarkdownWriterBase"/> defaults as-is;
    /// CommonMark-specific behavior (HTML inline tags, intraword spacing, etc.) lands as
    /// overrides in a later phase.
    /// </summary>
    public sealed class CommonMarkWriter : MarkdownWriterBase
    {
        public CommonMarkWriter(Config config) : base(config)
        {
        }
    }
}
