
using System.Linq;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Em : ConverterBase
    {
        public Em(Converter converter) : base(converter)
        {
            var elements = new [] { "em", "i" };

            foreach (var element in elements)
            {
                Converter.Register(element, this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node);

            if (string.IsNullOrWhiteSpace(content) || AlreadyItalic(node))
            {
                return content;
            }

            var spaceSuffix = (node.NextSibling?.Name == "i" || node.NextSibling?.Name == "em")
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
