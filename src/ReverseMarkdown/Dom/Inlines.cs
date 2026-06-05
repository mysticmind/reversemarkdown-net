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

    /// <summary>Strikethrough (<c>s</c>, <c>del</c>, <c>strike</c>).</summary>
    public sealed class MdStrikethrough : MdInline, IInlineSink
    {
        public MdStrikethrough()
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

    /// <summary>A hyperlink (<c>a</c>) with inline content.</summary>
    public sealed class MdLink : MdInline, IInlineSink
    {
        public MdLink(string url)
        {
            Url = url;
            Children = new MdNodeList<MdInline>(this);
        }

        public string Url { get; set; }
        public string? Title { get; set; }

        public MdNodeList<MdInline> Children { get; }

        void IInlineSink.Add(MdInline inline) => Children.Add(inline);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>Superscript (<c>sup</c>).</summary>
    public sealed class MdSuperscript : MdInline, IInlineSink
    {
        public MdSuperscript()
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

    /// <summary>Subscript (<c>sub</c>).</summary>
    public sealed class MdSubscript : MdInline, IInlineSink
    {
        public MdSubscript()
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

    /// <summary>An image (<c>img</c>). Leaf node.</summary>
    public sealed class MdImage : MdInline
    {
        public MdImage(string url)
        {
            Url = url;
        }

        public string Url { get; set; }
        public string? Title { get; set; }
        public string Alt { get; set; } = string.Empty;

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }

    /// <summary>Inline code (<c>code</c> outside a <c>pre</c>). Leaf node.</summary>
    public sealed class MdInlineCode : MdInline
    {
        public MdInlineCode(string literal)
        {
            Literal = literal;
        }

        public string Literal { get; set; }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }

    /// <summary>A line break (<c>br</c>). Leaf node.</summary>
    public sealed class MdLineBreak : MdInline
    {
        public bool Hard { get; set; } = true;

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }

    /// <summary>Inline or display math. <see cref="Literal"/> is the bare TeX (delimiters stripped).</summary>
    public sealed class MdMath : MdInline
    {
        public MdMath(string literal, bool display)
        {
            Literal = literal;
            Display = display;
        }

        public string Literal { get; set; }
        public bool Display { get; set; }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }

    /// <summary>A citation (<c>cite</c> / <c>data-cite</c>) with display text and optional key.</summary>
    public sealed class MdCitation : MdInline, IInlineSink
    {
        public MdCitation()
        {
            Children = new MdNodeList<MdInline>(this);
        }

        public string? Key { get; set; }

        public MdNodeList<MdInline> Children { get; }

        void IInlineSink.Add(MdInline inline) => Children.Add(inline);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>A footnote reference (e.g. <c>[^1]</c>). Leaf node.</summary>
    public sealed class MdFootnoteReference : MdInline
    {
        public MdFootnoteReference(string id)
        {
            Id = id;
        }

        public string Id { get; set; }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }

    /// <summary>A Pandoc bracketed span (<c>span</c> with class/id) with inline content.</summary>
    public sealed class MdBracketedSpan : MdInline, IInlineSink
    {
        public MdBracketedSpan()
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

    /// <summary>Verbatim inline HTML — the inline escape hatch for unrepresentable input.</summary>
    public sealed class MdRawInline : MdInline
    {
        public MdRawInline(string html)
        {
            Html = html;
        }

        public string Html { get; set; }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }
}
