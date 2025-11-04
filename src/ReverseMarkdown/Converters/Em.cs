using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Em : ConverterBase
    {
        public Em(Converter converter) : base(converter)
        {
            Converter.Register("em", this);
            Converter.Register("i", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node);

            if (string.IsNullOrWhiteSpace(content) || AlreadyItalic(node))
            {
                return content;
            }

            var spaceSuffix = node.NextSibling?.Name is "i" or "em"
                ? " "
                : "";

            var emphasis = Converter.Config.SlackFlavored ? "_" : "*";
            return content.EmphasizeContentWhitespaceGuard(emphasis, spaceSuffix);
        }

        private bool AlreadyItalic(HtmlNode node)
        {
            return Context.AncestorsAny("i") || Context.AncestorsAny("em");
        }
    }
}
