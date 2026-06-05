namespace ReverseMarkdown.Dom
{
    /// <summary>
    /// Visitor over the Markdown DOM. Writers implement this; one overload per node type.
    /// As new node types are added in later phases, add a method here and a default in
    /// <c>MarkdownWriterBase</c> so every writer keeps compiling.
    /// </summary>
    public interface IMdVisitor
    {
        void Visit(MarkdownDocument node);
        void Visit(MdHeading node);
        void Visit(MdParagraph node);
        void Visit(MdText node);
        void Visit(MdStrong node);
        void Visit(MdEmphasis node);
    }
}
