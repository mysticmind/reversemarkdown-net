using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class ByPass : ConverterBase {
        public ByPass(Converter converter) : base(converter)
        {
            Converter.Register("#document", this);
            Converter.Register("html", this);
            Converter.Register("body", this);
            Converter.Register("span", this);
            Converter.Register("thead", this);
            Converter.Register("tbody", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            TreatChildren(writer, node);
        }
    }
}
