using System.Collections.Generic;
using System.Linq;

namespace ReverseMarkdown.Dom
{
    /// <summary>A literal text run.</summary>
    public sealed class MdText : MdInline
    {
        public MdText(string value)
        {
            Value = value;
        }

        public string Value { get; set; }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }

    /// <summary>Strong emphasis (<c>strong</c>, <c>b</c>).</summary>
    public sealed class MdStrong : MdInline, IInlineSink
    {
        public MdStrong()
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

    /// <summary>Emphasis (<c>em</c>, <c>i</c>).</summary>
    public sealed class MdEmphasis : MdInline, IInlineSink
    {
        public MdEmphasis()
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
