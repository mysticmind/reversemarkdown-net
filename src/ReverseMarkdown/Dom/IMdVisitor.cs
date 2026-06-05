namespace ReverseMarkdown.Dom
{
    /// <summary>
    /// Visitor over the Markdown DOM. Writers implement this; one overload per node type.
    /// As new node types are added in later phases, add a method here and a default in
    /// <c>MarkdownWriterBase</c> so every writer keeps compiling.
    /// </summary>
    public interface IMdVisitor
    {
        // Blocks
        void Visit(MarkdownDocument node);
        void Visit(MdHeading node);
        void Visit(MdParagraph node);
        void Visit(MdThematicBreak node);
        void Visit(MdBlockquote node);
        void Visit(MdList node);
        void Visit(MdListItem node);
        void Visit(MdCodeBlock node);
        void Visit(MdTable node);
        void Visit(MdTableRow node);
        void Visit(MdTableCell node);
        void Visit(MdDefinitionList node);
        void Visit(MdDefinitionTerm node);
        void Visit(MdDefinitionDescription node);
        void Visit(MdHtmlBlock node);

        // Inlines
        void Visit(MdText node);
        void Visit(MdStrong node);
        void Visit(MdEmphasis node);
        void Visit(MdStrikethrough node);
        void Visit(MdSuperscript node);
        void Visit(MdSubscript node);
        void Visit(MdLink node);
        void Visit(MdImage node);
        void Visit(MdInlineCode node);
        void Visit(MdLineBreak node);
        void Visit(MdRawInline node);
    }
}
