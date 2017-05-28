using System.Linq;
using HtmlAgilityPack;
using static System.Environment;

namespace ReverseMarkdown.Converters
{
    public class P
        : ConverterBase
    {
        public P(Converter converter)
            : base(converter)
        {
            Converter.Register("p", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"{IndentationFor(node)}{TreatChildren(node).Trim()}{NewLine}{NewLine}";
        }

        private static string IndentationFor(HtmlNode node)
        {
            var length = node
                .Ancestors("ol")
                .Count() + node.Ancestors("ul").Count();

            return node.ParentNode.Name.ToLowerInvariant() == "li" && node
                .ParentNode
                .FirstChild != node
                ? new string(' ', length * 4)
                : NewLine + NewLine;
        }
    }
}