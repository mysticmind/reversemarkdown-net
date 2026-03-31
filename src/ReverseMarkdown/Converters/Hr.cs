using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Hr : ConverterBase {
        public Hr(Converter converter) : base(converter)
        {
            Converter.Register("hr", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.SlackFlavored) {
                throw new SlackUnsupportedTagException(node.Name);
            }

            if (Converter.Config.TelegramMarkdownV2) {
                writer.WriteLine();
                writer.Write("\\-\\-\\-");
                writer.WriteLine();
                return;
            }

            writer.WriteLine();
            writer.Write("* * *");
            writer.WriteLine();
        }
    }
}
