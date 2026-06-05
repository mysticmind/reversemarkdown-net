using HtmlAgilityPack;

namespace ReverseMarkdown.Readers
{
    /// <summary>
    /// Builds Markdown DOM node(s) from an HTML node. Readers are <b>flavor-agnostic</b>:
    /// they always produce the richest applicable node and never inspect <c>Config</c> flavor
    /// flags. Flavor choices and degradation are the writer's job.
    /// </summary>
    public interface IMdReader
    {
        void Read(HtmlNode node, ReaderContext ctx);
    }
}
