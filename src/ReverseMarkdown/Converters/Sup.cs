using System.IO;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class Sup : ConverterBase {
        public Sup(Converter converter) : base(converter)
        {
            Converter.Register("sup", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.SlackFlavored) {
                throw new SlackUnsupportedTagException(node.Name);
            }

            var content = TreatChildrenAsString(node);

            if (string.IsNullOrEmpty(content) || AlreadySup()) {
                writer.Write(content);
                return;
            }

            writer.Write('^');
            writer.Write(content.Chomp());
            writer.Write('^');
        }

        private bool AlreadySup()
        {
            return Context.AncestorsAny("sup");
        }
    }
}
