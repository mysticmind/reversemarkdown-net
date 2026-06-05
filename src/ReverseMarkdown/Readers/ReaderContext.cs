using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using ReverseMarkdown.Dom;

namespace ReverseMarkdown.Readers
{
    /// <summary>
    /// Per-<c>Parse</c> state threaded explicitly through the readers (replacing v5's
    /// <c>AsyncLocal</c> converter context). Owns the open-container cursor and the
    /// document-level collectors. Readers are stateless; all state lives here.
    /// </summary>
    public sealed class ReaderContext
    {
        private readonly MarkdownDomReader _reader;
        private readonly Stack<MdNode> _open = new();

        internal ReaderContext(MarkdownDomReader reader, MarkdownDocument document)
        {
            _reader = reader;
            Document = document;
            _open.Push(document);
        }

        public MarkdownDocument Document { get; }

        /// <summary>The container currently being built.</summary>
        public MdNode Current => _open.Peek();

        /// <summary>True when the current container accepts inline content directly.</summary>
        public bool CurrentAcceptsInline => Current is IInlineSink;

        /// <summary>Push <paramref name="container"/> as the current container until disposed.</summary>
        public IDisposable Open(MdNode container) => new Scope(this, container);

        /// <summary>Attach a block to the current container.</summary>
        public void Emit(MdBlock block)
        {
            if (Current is IBlockSink sink)
            {
                sink.Add(block);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot emit block '{block.GetType().Name}' into '{Current.GetType().Name}'.");
            }
        }

        /// <summary>
        /// Attach an inline to the current container. If the current container only accepts
        /// blocks, wrap the inline in a paragraph (keeps loose inline content valid).
        /// </summary>
        public void Emit(MdInline inline)
        {
            switch (Current)
            {
                case IInlineSink inlineSink:
                    inlineSink.Add(inline);
                    break;
                case IBlockSink blockSink:
                    var paragraph = new MdParagraph();
                    ((IInlineSink)paragraph).Add(inline);
                    blockSink.Add(paragraph);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Cannot emit inline '{inline.GetType().Name}' into '{Current.GetType().Name}'.");
            }
        }

        /// <summary>Read every child of <paramref name="node"/> into the current container.</summary>
        public void ReadChildren(HtmlNode node)
        {
            if (!node.HasChildNodes)
            {
                return;
            }

            foreach (var child in node.ChildNodes)
            {
                _reader.ReadNode(child, this);
            }
        }

        private sealed class Scope : IDisposable
        {
            private readonly ReaderContext _ctx;

            public Scope(ReaderContext ctx, MdNode container)
            {
                _ctx = ctx;
                ctx._open.Push(container);
            }

            public void Dispose() => _ctx._open.Pop();
        }
    }
}
