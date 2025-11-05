using System.IO;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class Img : ConverterBase {
        public Img(Converter converter) : base(converter)
        {
            Converter.Register("img", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.SlackFlavored) {
                throw new SlackUnsupportedTagException(node.Name);
            }

            var alt = node.GetAttributeValue("alt", string.Empty);
            var src = node.GetAttributeValue("src", string.Empty);
            var scheme = StringUtils.GetScheme(src);

            if (!Converter.Config.IsSchemeWhitelisted(scheme)) {
                return;
            }

            writer.Write("![");
            writer.Write(StringUtils.EscapeLinkText(alt));
            writer.Write("](");
            writer.Write(src);

            if (ExtractTitle(node) is { Length: > 0 } title) {
                writer.Write(" \"");
                writer.Write(title);
                writer.Write("\"");
            }

            writer.Write(')');
        }
    }
}
