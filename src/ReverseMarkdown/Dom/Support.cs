using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ReverseMarkdown.Dom
{
    /// <summary>A node that accepts block children during reading.</summary>
    internal interface IBlockSink
    {
        void Add(MdBlock block);
    }

    /// <summary>A node that accepts inline children during reading.</summary>
    internal interface IInlineSink
    {
        void Add(MdInline inline);
    }

    /// <summary>
    /// Shared remove/replace logic over a typed child <see cref="Collection{T}"/>, used by
    /// container nodes to implement <see cref="MdNode.RemoveChildCore"/> /
    /// <see cref="MdNode.ReplaceChildCore"/>.
    /// </summary>
    internal static class MdChildOps
    {
        public static bool Remove<T>(Collection<T> children, MdNode child) where T : MdNode
        {
            return child is T typed && children.Remove(typed);
        }

        public static bool Replace<T>(Collection<T> children, MdNode oldChild, IReadOnlyList<MdNode> replacements)
            where T : MdNode
        {
            if (oldChild is not T oldTyped)
            {
                return false;
            }

            var index = children.IndexOf(oldTyped);
            if (index < 0)
            {
                return false;
            }

            children.RemoveAt(index);

            for (var i = 0; i < replacements.Count; i++)
            {
                if (replacements[i] is not T replacement)
                {
                    throw new InvalidOperationException(
                        $"Cannot insert a '{replacements[i].GetType().Name}' into a '{typeof(T).Name}' child collection.");
                }

                children.Insert(index + i, replacement);
            }

            return true;
        }
    }

    /// <summary>Id / class / key-value attributes carried from the source HTML.</summary>
    public sealed class MdAttributes
    {
        public string? Id { get; set; }
        public IList<string> Classes { get; } = new List<string>();
        public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Document-level side-channel data collected during reading and emitted by the writer
    /// at fixed positions (footnote definitions at the end, metadata at the top, …).
    /// </summary>
    public sealed class MdDocumentMeta
    {
        public IList<KeyValuePair<string, string>> Metadata { get; } = new List<KeyValuePair<string, string>>();
        public IDictionary<string, string> Abbreviations { get; } = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>Footnote definitions collected during reading, emitted at document end.</summary>
        public IList<MdFootnoteDefinition> Footnotes { get; } = new List<MdFootnoteDefinition>();
    }
}
