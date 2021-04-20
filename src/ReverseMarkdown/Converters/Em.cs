
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

            if (string.IsNullOrEmpty(content.Trim()) || AlreadyItalic(node))
            {
                return content;
            }
            else
            {
                var spaceSuffix = (node.NextSibling?.Name == "i" || node.NextSibling?.Name == "em")
                    ? " "
                    : "";

                return $"*{content.Trim()}*{spaceSuffix}";
            }
        }

        private static bool AlreadyItalic(HtmlNode node)
        {
            return node.Ancestors("i").Any() || node.Ancestors("em").Any();
        }
    }
}
