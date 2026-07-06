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

    /// <summary>A thematic break (<c>hr</c>). Leaf block.</summary>
    public sealed class MdThematicBreak : MdBlock
    {
        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => System.Linq.Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }

    /// <summary>A block quote (<c>blockquote</c>) holding block children.</summary>
    public sealed class MdBlockquote : MdBlock, IBlockSink
    {
        public MdBlockquote()
        {
            Children = new MdNodeList<MdBlock>(this);
        }

        public MdNodeList<MdBlock> Children { get; }

        void IBlockSink.Add(MdBlock block) => Children.Add(block);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>An ordered (<c>ol</c>) or unordered (<c>ul</c>) list.</summary>
    public sealed class MdList : MdBlock, IBlockSink
    {
        public MdList()
        {
            Items = new MdNodeList<MdListItem>(this);
        }

        public bool Ordered { get; set; }
        public int Start { get; set; } = 1;
        public bool Tight { get; set; } = true;

        public MdNodeList<MdListItem> Items { get; }

        void IBlockSink.Add(MdBlock block)
        {
            if (block is MdListItem item)
            {
                Items.Add(item);
            }
            else if (Items.Count > 0)
            {
                // Stray non-li block that is a direct child of the list (e.g. a <p> or nested
                // <ol>/<ul> placed between <li>s rather than inside one): attach it to the
                // preceding item as continuation content, matching how browsers associate it.
                ((IBlockSink)Items[Items.Count - 1]).Add(block);
            }
            else
            {
                // No preceding item: wrap it in an item to keep the tree valid.
                var wrapper = new MdListItem();
                ((IBlockSink)wrapper).Add(block);
                Items.Add(wrapper);
            }
        }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Items;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Items, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Items, oldChild, newChildren);
    }

    /// <summary>A list item (<c>li</c>) holding block children. <see cref="Checked"/> is set for task lists.</summary>
    public sealed class MdListItem : MdBlock, IBlockSink
    {
        public MdListItem()
        {
            Children = new MdNodeList<MdBlock>(this);
        }

        public bool? Checked { get; set; }

        public MdNodeList<MdBlock> Children { get; }

        void IBlockSink.Add(MdBlock block) => Children.Add(block);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>A code block (<c>pre</c> / <c>pre&gt;code</c>). Leaf block.</summary>
    public sealed class MdCodeBlock : MdBlock
    {
        public MdCodeBlock(string literal)
        {
            Literal = literal;
        }

        public string Literal { get; set; }
        public string? Language { get; set; }
        public bool LanguageIsAttribute { get; set; }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => System.Linq.Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }

    /// <summary>Column text alignment for table cells.</summary>
    public enum ColumnAlignment
    {
        None,
        Left,
        Center,
        Right,
    }

    /// <summary>A table (<c>table</c>). Holds rows plus an optional caption.</summary>
    public sealed class MdTable : MdBlock, IBlockSink
    {
        public MdTable()
        {
            Rows = new MdNodeList<MdTableRow>(this);
        }

        public string? Caption { get; set; }

        public MdNodeList<MdTableRow> Rows { get; }

        void IBlockSink.Add(MdBlock block)
        {
            if (block is MdTableRow row)
            {
                Rows.Add(row);
            }
        }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Rows;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Rows, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Rows, oldChild, newChildren);
    }

    /// <summary>A table row (<c>tr</c>). <see cref="IsHeader"/> marks header rows.</summary>
    public sealed class MdTableRow : MdBlock, IBlockSink
    {
        public MdTableRow()
        {
            Cells = new MdNodeList<MdTableCell>(this);
        }

        public bool IsHeader { get; set; }

        public MdNodeList<MdTableCell> Cells { get; }

        void IBlockSink.Add(MdBlock block)
        {
            if (block is MdTableCell cell)
            {
                Cells.Add(cell);
            }
        }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Cells;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Cells, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Cells, oldChild, newChildren);
    }

    /// <summary>A table cell (<c>td</c> / <c>th</c>) holding block content.</summary>
    public sealed class MdTableCell : MdBlock, IBlockSink
    {
        public MdTableCell()
        {
            Children = new MdNodeList<MdBlock>(this);
        }

        public ColumnAlignment Align { get; set; } = ColumnAlignment.None;

        public MdNodeList<MdBlock> Children { get; }

        void IBlockSink.Add(MdBlock block) => Children.Add(block);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>A definition list (<c>dl</c>) holding terms and descriptions in order.</summary>
    public sealed class MdDefinitionList : MdBlock, IBlockSink
    {
        public MdDefinitionList()
        {
            Items = new MdNodeList<MdBlock>(this);
        }

        /// <summary>Sequence of <see cref="MdDefinitionTerm"/> / <see cref="MdDefinitionDescription"/>.</summary>
        public MdNodeList<MdBlock> Items { get; }

        void IBlockSink.Add(MdBlock block)
        {
            if (block is MdDefinitionTerm or MdDefinitionDescription)
            {
                Items.Add(block);
            }
        }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Items;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Items, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Items, oldChild, newChildren);
    }

    /// <summary>A definition term (<c>dt</c>) with inline content.</summary>
    public sealed class MdDefinitionTerm : MdBlock, IInlineSink
    {
        public MdDefinitionTerm()
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

    /// <summary>A definition description (<c>dd</c>) holding block content.</summary>
    public sealed class MdDefinitionDescription : MdBlock, IBlockSink
    {
        public MdDefinitionDescription()
        {
            Children = new MdNodeList<MdBlock>(this);
        }

        public MdNodeList<MdBlock> Children { get; }

        void IBlockSink.Add(MdBlock block) => Children.Add(block);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>A footnote definition (collected into <see cref="MdDocumentMeta"/>, emitted at end).</summary>
    public sealed class MdFootnoteDefinition : MdBlock, IBlockSink
    {
        public MdFootnoteDefinition(string id)
        {
            Id = id;
            Children = new MdNodeList<MdBlock>(this);
        }

        public string Id { get; set; }

        public MdNodeList<MdBlock> Children { get; }

        void IBlockSink.Add(MdBlock block) => Children.Add(block);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>A Pandoc fenced div (<c>div</c> with class/id) holding block content.</summary>
    public sealed class MdFencedDiv : MdBlock, IBlockSink
    {
        public MdFencedDiv()
        {
            Children = new MdNodeList<MdBlock>(this);
        }

        public MdNodeList<MdBlock> Children { get; }

        void IBlockSink.Add(MdBlock block) => Children.Add(block);

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => Children;

        internal override bool RemoveChildCore(MdNode child) => MdChildOps.Remove(Children, child);

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren)
            => MdChildOps.Replace(Children, oldChild, newChildren);
    }

    /// <summary>A Pandoc line block (<c>div.line-block</c>): inline content with line breaks.</summary>
    public sealed class MdLineBlock : MdBlock, IInlineSink
    {
        public MdLineBlock()
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

    /// <summary>Verbatim block-level HTML — the block escape hatch for unrepresentable input.</summary>
    public sealed class MdHtmlBlock : MdBlock
    {
        public MdHtmlBlock(string html)
        {
            Html = html;
        }

        public string Html { get; set; }

        public override void Accept(IMdVisitor visitor) => visitor.Visit(this);

        protected internal override IEnumerable<MdNode> EnumerateChildren() => System.Linq.Enumerable.Empty<MdNode>();

        internal override bool RemoveChildCore(MdNode child) => false;

        internal override bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren) => false;
    }
}
