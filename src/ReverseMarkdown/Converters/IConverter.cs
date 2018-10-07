using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public interface IConverter
    {
        string Convert(HtmlNode node);
    }
}
