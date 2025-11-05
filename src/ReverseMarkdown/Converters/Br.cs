using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Br : ConverterBase {
        public Br(Converter converter) : base(converter)
        {
            Converter.Register("br", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (node.ParentNode.Name is "strong" or "b" or "em" or "i") {
                return;
            }

            if (Converter.Config.GithubFlavored) {
                writer.WriteLine();
            }
            else {
                writer.WriteLine("  ");
            }
        }
    }
}
