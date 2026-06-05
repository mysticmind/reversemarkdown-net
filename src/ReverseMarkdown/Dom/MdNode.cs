using System;
using System.Collections.Generic;
using System.Linq;

namespace ReverseMarkdown.Dom
{
    /// <summary>
    /// Base type for every node in the Markdown DOM (the v6 intermediate representation).
    /// The tree is mutable: see <see cref="Remove"/>, <see cref="ReplaceWith"/> and the
    /// child collections on container nodes (which maintain <see cref="Parent"/>).
    /// </summary>
    public abstract class MdNode
    {
        /// <summary>The parent node, or null for a detached node / the document root.</summary>
        public MdNode? Parent { get; internal set; }

        /// <summary>Optional id / class / key-value attributes (Pandoc syntax, filtering).</summary>
        public MdAttributes? Attributes { get; set; }

        /// <summary>The originating HTML tag name, for diagnostics and filters. May be null.</summary>
        public string? SourceTag { get; init; }

        /// <summary>Double-dispatch entry point for writers and other visitors.</summary>
        public abstract void Accept(IMdVisitor visitor);

        /// <summary>Direct child nodes in document order (empty for leaf nodes).</summary>
        protected internal abstract IEnumerable<MdNode> EnumerateChildren();

        internal abstract bool RemoveChildCore(MdNode child);

        internal abstract bool ReplaceChildCore(MdNode oldChild, IReadOnlyList<MdNode> newChildren);

        /// <summary>Detach this node from its parent. No-op if already detached.</summary>
        public void Remove()
        {
            Parent?.RemoveChildCore(this);
        }

        /// <summary>
        /// Replace this node, in place, with zero or more nodes. Replacements must be
        /// assignable to the parent's child kind (block vs inline) or this throws.
        /// </summary>
        public void ReplaceWith(params MdNode[] replacements)
        {
            if (Parent is null)
            {
                throw new InvalidOperationException("Cannot replace a node that has no parent.");
            }

            Parent.ReplaceChildCore(this, replacements);
        }

        /// <summary>All descendant nodes, depth-first, in document order.</summary>
        public IEnumerable<MdNode> Descendants()
        {
            foreach (var child in EnumerateChildren())
            {
                yield return child;
                foreach (var d in child.Descendants())
                {
                    yield return d;
                }
            }
        }

        /// <summary>This node followed by all of its descendants.</summary>
        public IEnumerable<MdNode> DescendantsAndSelf()
        {
            yield return this;
            foreach (var d in Descendants())
            {
                yield return d;
            }
        }

        /// <summary>Ancestors from the immediate parent up to the root.</summary>
        public IEnumerable<MdNode> Ancestors()
        {
            var p = Parent;
            while (p is not null)
            {
                yield return p;
                p = p.Parent;
            }
        }

        /// <summary>
        /// Remove every descendant matching <paramref name="predicate"/> (Markdown-side
        /// filtering for issue #79 — "pick what I don't want"). Returns the number removed.
        /// </summary>
        public int RemoveWhere(Func<MdNode, bool> predicate)
        {
            var matches = Descendants().Where(predicate).ToList();
            foreach (var match in matches)
            {
                match.Remove();
            }

            return matches.Count;
        }
    }

    /// <summary>A block-level Markdown node (heading, paragraph, list, …).</summary>
    public abstract class MdBlock : MdNode { }

    /// <summary>An inline Markdown node (text, emphasis, link, …).</summary>
    public abstract class MdInline : MdNode { }
}
