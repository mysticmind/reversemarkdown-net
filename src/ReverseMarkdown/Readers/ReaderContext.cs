using System;
using System.Collections.Generic;
using AngleSharp.Dom;
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
        private readonly Stack<Frame> _frames = new();

        internal ReaderContext(MarkdownDomReader reader, MarkdownDocument document, Config config)
        {
            _reader = reader;
            Document = document;
            Config = config;
            _frames.Push(new Frame(document));
        }

        public MarkdownDocument Document { get; }

        /// <summary>The active configuration (scheme whitelist, smart-href, base64, …).</summary>
        public Config Config { get; }

        private int _imageIndex;

        /// <summary>Next zero-based image index for this conversion (base64 SaveToFile naming).</summary>
        public int NextImageIndex() => _imageIndex++;

        /// <summary>The container currently being built.</summary>
        public MdNode Current => _frames.Peek().Container;

        /// <summary>True when the current container accepts inline content directly.</summary>
        public bool CurrentAcceptsInline => Current is IInlineSink;

        /// <summary>Push <paramref name="container"/> as the current container until disposed.</summary>
        public IDisposable Open(MdNode container) => new Scope(this, container);

        /// <summary>Attach a block to the current container, closing any open implicit paragraph.</summary>
        public void Emit(MdBlock block)
        {
            var frame = _frames.Peek();
            if (frame.Container is IBlockSink sink)
            {
                sink.Add(block);
                frame.ImplicitParagraph = null;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot emit block '{block.GetType().Name}' into '{frame.Container.GetType().Name}'.");
            }
        }

        /// <summary>
        /// Attach an inline to the current container. If the container only accepts blocks,
        /// the inline accrues into a single implicit paragraph (consecutive loose inline content
        /// coalesces; an intervening block starts a fresh paragraph).
        /// </summary>
        public void Emit(MdInline inline)
        {
            var frame = _frames.Peek();
            switch (frame.Container)
            {
                case IInlineSink inlineSink:
                    inlineSink.Add(inline);
                    break;
                case IBlockSink blockSink:
                    if (frame.ImplicitParagraph is null)
                    {
                        frame.ImplicitParagraph = new MdParagraph();
                        blockSink.Add(frame.ImplicitParagraph);
                    }

                    ((IInlineSink)frame.ImplicitParagraph).Add(inline);
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Cannot emit inline '{inline.GetType().Name}' into '{frame.Container.GetType().Name}'.");
            }
        }

        /// <summary>Read every child of <paramref name="node"/> into the current container.</summary>
        public void ReadChildren(INode node)
        {
            foreach (var child in node.ChildNodes)
            {
                _reader.ReadNode(child, this);
            }
        }

        private sealed class Frame
        {
            public Frame(MdNode container)
            {
                Container = container;
            }

            public MdNode Container { get; }

            public MdParagraph? ImplicitParagraph { get; set; }
        }

        private sealed class Scope : IDisposable
        {
            private readonly ReaderContext _ctx;

            public Scope(ReaderContext ctx, MdNode container)
            {
                _ctx = ctx;
                ctx._frames.Push(new Frame(container));
            }

            public void Dispose() => _ctx._frames.Pop();
        }
    }
}
