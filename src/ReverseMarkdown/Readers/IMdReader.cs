using AngleSharp.Dom;

namespace ReverseMarkdown.Readers
{
    /// <summary>
    /// Builds Markdown DOM node(s) from an HTML element. Readers are <b>flavor-agnostic</b>:
    /// they always produce the richest applicable node and never inspect <c>Config</c> flavor
    /// flags. Flavor choices and degradation are the writer's job.
    /// <para>
    /// The v6 reader path uses AngleSharp's HTML5-compliant DOM (<see cref="IElement"/>);
    /// text and comment nodes are dispatched by <see cref="MarkdownDomReader"/> itself.
    /// </para>
    /// </summary>
    public interface IMdReader
    {
        void Read(IElement element, ReaderContext ctx);
    }
}
