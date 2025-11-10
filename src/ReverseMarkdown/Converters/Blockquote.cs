using System.IO;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class Blockquote : ConverterBase {
        public Blockquote(Converter converter) : base(converter)
        {
            Converter.Register("blockquote", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            writer.WriteLine();
            writer.WriteLine();

            var content = TreatChildrenAsString(node);
            foreach (var line in content.ReadLines()) {
                writer.Write('>');
                writer.Write(' ');
                writer.WriteLine(line);
            }

            writer.WriteLine();
        }
    }
}
