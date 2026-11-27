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

        /// <summary>True when inline content is currently flowing: either the container accepts
        /// inline directly, or a block-level container has an open implicit paragraph accruing
        /// inline. Whitespace-only text is content in this state (e.g. a whitespace <span> between
        /// two inline runs) rather than droppable block-separator whitespace.</summary>
        public bool InInlineFlow => CurrentAcceptsInline || _frames.Peek().ImplicitParagraph is not null;

        /// <summary>When true, inline elements are converted to markdown rather than passed through
        /// as raw HTML — used while reading a figcaption captured as an image's alt, where the
        /// content must be markdown (e.g. a link as <c>[x](y)</c>, not raw <c>&lt;a&gt;</c>).</summary>
        public bool ForceMarkdownInline { get; set; }

        /// <summary>True while reading a table cell's content. A nested table/list inside a cell
        /// can't be represented as markdown, so it is emitted as compacted raw HTML instead.</summary>
        public bool InTableCell { get; set; }

        /// <summary>Push <paramref name="container"/> as the current container until disposed.</summary>
        public IDisposable Open(MdNode container) => new Scope(this, container);

        /// <summary>Attach a block to the current container, closing any open implicit paragraph.</summary>
        public void Emit(MdBlock block)
        {
            var frame = _frames.Peek();
            if (frame.Container is IBlockSink sink)
            {
                // A block sibling closes any open implicit paragraph: the newline whitespace at the
                // paragraph's trailing edge is block-separator whitespace (e.g. the "\n\n" between a
                // bare-text <li> run and a following <pre>), not content, so drop it.
                TrimTrailingWhitespace(frame.ImplicitParagraph);
                sink.Add(block);
                frame.ImplicitParagraph = null;
            }
            else if (frame.Container is IInlineSink inlineSink)
            {
                // A block nested inside an inline element (e.g. <del><p>..</p></del>, which the HTML
                // parser does allow) cannot hold a block. Degrade by flattening the block's own
                // inline content into the inline container rather than throwing. The block's edges
                // are word boundaries, so its text must not run into the text around it.
                frame.PendingBlockBreak = true;
                foreach (var child in new List<MdNode>(block.EnumerateChildren()))
                {
                    if (child is MdInline childInline)
                    {
                        AddInline(frame, inlineSink, childInline);
                    }
                    else if (child is MdBlock childBlock)
                    {
                        Emit(childBlock);
                    }
                }

                frame.PendingBlockBreak = true;
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
                    AddInline(frame, inlineSink, inline);
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

        /// <summary>Close the current container's open implicit paragraph so following inline
        /// content starts a new one. Block-level wrappers (div/section/aside/…) use this around
        /// their content so adjacent wrappers render as separate blocks rather than running
        /// together into one paragraph.</summary>
        public void FlushImplicitParagraph()
        {
            var frame = _frames.Peek();
            TrimTrailingWhitespace(frame.ImplicitParagraph);
            frame.ImplicitParagraph = null;

            // A block-level wrapper read straight into an inline container (e.g. <a><div>..</div>
            // <div>..</div></a>) has no paragraph to close; its edges are still word boundaries.
            if (frame.Container is IInlineSink)
            {
                frame.PendingBlockBreak = true;
            }
        }

        // Add an inline to an inline container. After a block boundary flattened into that
        // container, separate the text on either side with a space, unless one side already
        // has whitespace there or there is no text before it (a leading block needs none).
        private static void AddInline(Frame frame, IInlineSink sink, MdInline inline)
        {
            if (frame.PendingBlockBreak)
            {
                var next = EdgeIsWhitespace(inline, atEnd: false);
                if (next is null)
                {
                    // No text in this inline; the boundary still applies to what follows it.
                    sink.Add(inline);
                    return;
                }

                frame.PendingBlockBreak = false;
                if (next == false && EdgeIsWhitespace(frame.Container, atEnd: true) == false)
                {
                    sink.Add(new MdText(" "));
                }
            }

            sink.Add(inline);
        }

        // Whether the first (or last) character of a node's inline content is whitespace, or null
        // when the node has no content at that edge (an empty run or an empty container).
        private static bool? EdgeIsWhitespace(MdNode node, bool atEnd)
        {
            switch (node)
            {
                case MdText text:
                    return text.Value.Length == 0
                        ? null
                        : char.IsWhiteSpace(atEnd ? text.Value[^1] : text.Value[0]);
                case MdLineBreak:
                    return true;
                case IInlineSink:
                    var children = new List<MdNode>(node.EnumerateChildren());
                    if (atEnd)
                    {
                        children.Reverse();
                    }

                    foreach (var child in children)
                    {
                        if (EdgeIsWhitespace(child, atEnd) is { } edge)
                        {
                            return edge;
                        }
                    }

                    return null;
                default:
                    // Images, code spans, raw HTML, math: visible content that is not whitespace.
                    return false;
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

        /// <summary>Finalize the root container, trimming its trailing implicit paragraph (the
        /// root frame never goes through <see cref="Scope"/> disposal, so it needs this directly).</summary>
        internal void FinalizeRoot() => TrimTrailingWhitespace(_frames.Peek().ImplicitParagraph);

        // Drop trailing whitespace newlines from the last text run of a closing implicit
        // paragraph; that whitespace separated it from a sibling block and is not content.
        private static void TrimTrailingWhitespace(MdParagraph? paragraph)
        {
            if (paragraph is null || paragraph.Children.Count == 0)
            {
                return;
            }

            if (paragraph.Children[^1] is MdText text)
            {
                text.Value = text.Value.TrimEnd(' ', '\t', '\r', '\n');
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

            /// <summary>True when a block boundary was flattened into this inline container and
            /// the next text added to it still needs separating from the text before it.</summary>
            public bool PendingBlockBreak { get; set; }
        }

        private sealed class Scope : IDisposable
        {
            private readonly ReaderContext _ctx;

            public Scope(ReaderContext ctx, MdNode container)
            {
                _ctx = ctx;
                ctx._frames.Push(new Frame(container));
            }

            public void Dispose()
            {
                // The container is closing; trim its trailing implicit paragraph the same way a
                // following block sibling would have (edge whitespace is not content).
                TrimTrailingWhitespace(_ctx._frames.Peek().ImplicitParagraph);
                _ctx._frames.Pop();
            }
        }
    }
}
