using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Writers
{
    /// <summary>Renders a Markdown DOM to a flavor-specific Markdown string.</summary>
    public interface IMarkdownWriter
    {
        string Write(MarkdownDocument document);
    }
}
