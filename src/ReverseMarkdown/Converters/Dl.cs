using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Dl : ConverterBase {
        public Dl(Converter converter) : base(converter)
        {
            Converter.Register("dl", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            writer.WriteLine();
            TreatChildren(writer, node);
            writer.WriteLine();
        }
    }
}
