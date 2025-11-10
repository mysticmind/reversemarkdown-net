using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Strong : ConverterBase {
        public Strong(Converter converter) : base(converter)
        {
            Converter.Register("strong", this);
            Converter.Register("b", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            var content = TreatChildrenAsString(node);

            if (string.IsNullOrEmpty(content) || AlreadyBold()) {
                writer.Write(content);
                return;
            }

            var spaceSuffix = node.NextSibling?.Name is "strong" or "b"
                ? " "
                : "";

            var emphasis = Converter.Config.SlackFlavored ? "*" : "**";
            TreatEmphasizeContentWhitespaceGuard(writer, content, emphasis, spaceSuffix);
        }

        private bool AlreadyBold()
        {
            return Context.AncestorsAny("strong") || Context.AncestorsAny("b");
        }
    }
}
