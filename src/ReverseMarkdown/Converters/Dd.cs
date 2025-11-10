using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Dd : ConverterBase {
        public Dd(Converter converter) : base(converter)
        {
            Converter.Register("dd", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            writer.Write(new string(' ', 4));
            writer.Write(Converter.Config.ListBulletChar);
            writer.Write(' ');
            var content = TreatChildrenAsString(node).Trim();
            writer.Write(content);
            writer.WriteLine();
        }
    }
}
