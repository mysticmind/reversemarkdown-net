using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Ignore : ConverterBase {
        public Ignore(Converter converter) : base(converter)
        {
            Converter.Register("colgroup", this);
            Converter.Register("col", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            // Do nothing, ignore the node
        }
    }
}
