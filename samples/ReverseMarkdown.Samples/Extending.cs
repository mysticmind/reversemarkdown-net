using ReverseMarkdown;
using ReverseMarkdown.Dom;
using ReverseMarkdown.Readers;

namespace Samples;

#region sample_plain_link_reader
// Render <a> as "text (href)" instead of a markdown link.
[MarkdownReader("a")]
public class PlainLinkReader : IMdReader
{
    public void Read(AngleSharp.Dom.IElement element, ReaderContext ctx)
    {
        var href = element.GetAttribute("href") ?? "";
        var text = element.TextContent.Trim();
        ctx.Emit(new MdText($"{text} ({href})") { SourceTag = "a" });
    }
}
#endregion

#region sample_strip_to_text
[MarkdownReader("strong", "b", "em", "i", "del", "s", "h1", "h2", "h3", "h4", "h5", "h6",
                "blockquote", "code", "pre", "span", "sub", "sup", "hr", "br", "img", "table")]
public class StripToTextReader : IMdReader
{
    public void Read(AngleSharp.Dom.IElement element, ReaderContext ctx) => ctx.ReadChildren(element);
}
#endregion

#region sample_highlight_reader
// Render <mark> as ==highlighted==. No [MarkdownReader] attribute: this one is registered
// explicitly, so nothing has to be discovered by reflection.
public class HighlightReader : IMdReader
{
    public void Read(AngleSharp.Dom.IElement element, ReaderContext ctx)
    {
        ctx.Emit(new MdRawInline("==") { SourceTag = element.LocalName });
        ctx.ReadChildren(element);
        ctx.Emit(new MdRawInline("==") { SourceTag = element.LocalName });
    }
}
#endregion

public static class Extending
{
    public static void Alias()
    {
        #region sample_alias
        var config = new Config();
        config.Tags.Aliases["u"] = "em"; // convert <u> as if it were <em>
        var converter = new Converter(config);
        #endregion
    }

    public static void CustomReader()
    {
        #region sample_custom_reader_wire
        var converter = new Converter(new Config(), typeof(PlainLinkReader).Assembly);
        #endregion
        _ = converter;
    }

    public static void RegisterReaderExplicitly()
    {
        #region sample_register_reader
        // Trimming / Native AOT safe: no attribute, no assembly scanning, no reflection.
        var converter = new Converter();
        converter.RegisterReader("mark", new HighlightReader());
        #endregion
        _ = converter;
    }

    public static void WhitelistToText()
    {
        #region sample_strip_to_text_usage
        var config = new Config
        {
            Tags = { Unknown = Config.UnknownTagsOption.Bypass }
        };
        var converter = new Converter(config, typeof(PlainLinkReader).Assembly);
        #endregion
        _ = converter;
    }

    public static void ParseRender()
    {
        var html = "<p>hi</p>";
        #region sample_parse_render
        var converter = new Converter();
        var document = converter.Parse(html);
        // inspect / filter / reshape the document here
        var markdown = converter.Render(document);
        #endregion
        _ = markdown;
    }
}
