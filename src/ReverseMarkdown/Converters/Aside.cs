using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Aside : ConverterBase {
        public Aside(Converter converter)
            : base(converter)
        {
            Converter.Register("aside", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            writer.WriteLine();
            TreatChildren(writer, node);
            writer.WriteLine();
        }
    }
}
