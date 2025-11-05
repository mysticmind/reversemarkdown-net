using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Em : ConverterBase {
        public Em(Converter converter) : base(converter)
        {
            Converter.Register("em", this);
            Converter.Register("i", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            var content = TreatChildrenAsString(node);

            if (string.IsNullOrWhiteSpace(content) || AlreadyItalic()) {
                writer.Write(content);
                return;
            }

            var spaceSuffix = node.NextSibling?.Name is "i" or "em"
                ? " "
                : string.Empty;

            var emphasis = Converter.Config.SlackFlavored ? "_" : "*";
            TreatEmphasizeContentWhitespaceGuard(writer, content, emphasis, spaceSuffix);
        }

        private bool AlreadyItalic()
        {
            return Context.AncestorsAny("i") || Context.AncestorsAny("em");
        }
    }
}
