using System.Collections.Generic;

namespace ReverseMarkdown.Dom
{
    /// <summary>The root of a Markdown DOM. Holds block children plus side-channel meta.</summary>
    public sealed class MarkdownDocument : MdBlock, IBlockSink
    {
        public MarkdownDocument()
        {
            Children = new MdNodeList<MdBlock>(this);
        }

        public MdNodeList<MdBlock> Children { get; }

        public MdDocumentMeta Meta { get; } = new();

        void IBlockSink.Add(MdBlock block) => Children.Add(block);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>A heading (<c>h1</c>–<c>h6</c>) with inline content.</summary>
    public sealed class MdHeading : MdBlock, IInlineSink
    {
        public MdHeading(int level)
        {
            Level = level;
            Children = new MdNodeList<MdInline>(this);
        }

        public int Level { get; set; }

        public MdNodeList<MdInline> Children { get; }

        void IInlineSink.Add(MdInline inline) => Children.Add(inline);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>A paragraph with inline content.</summary>
    public sealed class MdParagraph : MdBlock, IInlineSink
    {
        public MdParagraph()
        {
            Children = new MdNodeList<MdInline>(this);
        }

        public MdNodeList<MdInline> Children { get; }

        void IInlineSink.Add(MdInline inline) => Children.Add(inline);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }
}
