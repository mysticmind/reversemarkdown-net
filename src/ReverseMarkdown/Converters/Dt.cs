using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Dt : ConverterBase {
        public Dt(Converter converter) : base(converter)
        {
            Converter.Register("dt", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            writer.Write(Converter.Config.ListBulletChar);
            writer.Write(' ');
            var content = TreatChildrenAsString(node).Trim();
            writer.Write(content);
            writer.WriteLine();
        }
    }
}
