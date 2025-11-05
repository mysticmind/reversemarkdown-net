using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class PassThrough : ConverterBase {
        public PassThrough(Converter converter) : base(converter)
        {
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            writer.Write(node.OuterHtml);
        }
    }
}
