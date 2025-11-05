using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class S : ConverterBase {
        public S(Converter converter) : base(converter)
        {
            Converter.Register("s", this);
            Converter.Register("del", this);
            Converter.Register("strike", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            var content = TreatChildrenAsString(node);

            if (string.IsNullOrEmpty(content) || AlreadyStrikethrough()) {
                writer.Write(content);
                return;
            }

            var emphasis = Converter.Config.SlackFlavored ? "~" : "~~";
            TreatEmphasizeContentWhitespaceGuard(writer, content, emphasis);
        }

        private bool AlreadyStrikethrough()
        {
            return Context.AncestorsAny("s") || Context.AncestorsAny("del") || Context.AncestorsAny("strike");
        }
    }
}
