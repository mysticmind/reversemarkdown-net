using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public interface IConverter {
        void Convert(TextWriter writer, HtmlNode node);
    }
}
