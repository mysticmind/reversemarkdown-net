using System.Collections.Generic;
using System.Text;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Writers
{
    /// <summary>
    /// Base for flavor writers. Implements CommonMark-ish defaults; flavor writers override
    /// only the node renderings that differ and the <see cref="Degrade"/> hook for nodes the
    /// flavor cannot represent natively. Block separation is structural (decided here), not
    /// encoded in the tree — see docs/v6/architecture.md §5.
    /// </summary>
    public abstract class MarkdownWriterBase : IMdVisitor, IMarkdownWriter
    {
        protected MarkdownWriterBase(Config config)
        {
            Config = config;
        }

        protected Config Config { get; }

        protected StringBuilder Buffer { get; } = new();

        public virtual string Write(MarkdownDocument document)
        {
            Buffer.Clear();
            Visit(document);
            return Buffer.ToString();
        }

        public virtual void Visit(MarkdownDocument node) => WriteBlocks(node.Children);

        public virtual void Visit(MdHeading node)
        {
            Buffer.Append('#', node.Level).Append(' ');
            WriteInline(node.Children);
        }

        public virtual void Visit(MdParagraph node) => WriteInline(node.Children);

        public virtual void Visit(MdText node) => Buffer.Append(node.Value);

        public virtual void Visit(MdStrong node) => Wrap("**", node.Children);

        public virtual void Visit(MdEmphasis node) => Wrap("*", node.Children);

        /// <summary>Render block children separated by one blank line.</summary>
        protected void WriteBlocks(IEnumerable<MdBlock> blocks)
        {
            var first = true;
            foreach (var block in blocks)
            {
                if (!first)
                {
                    Buffer.Append("\n\n");
                }

                first = false;
                block.Accept(this);
            }
        }

        protected void WriteInline(IEnumerable<MdInline> inlines)
        {
            foreach (var inline in inlines)
            {
                inline.Accept(this);
            }
        }

        protected void Wrap(string delimiter, IEnumerable<MdInline> children)
        {
            Buffer.Append(delimiter);
            WriteInline(children);
            Buffer.Append(delimiter);
        }

        /// <summary>
        /// Fallback for a node a flavor cannot represent natively. Default emits the source
        /// tag as raw HTML when known; flavor writers may override to drop, throw, or inline.
        /// </summary>
        protected virtual void Degrade(MdNode node)
        {
            if (node.SourceTag is { Length: > 0 } tag)
            {
                Buffer.Append('<').Append(tag).Append("></").Append(tag).Append('>');
            }
        }
    }
}
