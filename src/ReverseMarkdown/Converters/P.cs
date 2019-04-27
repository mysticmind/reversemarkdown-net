using System;
using System.Linq;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class P : ConverterBase
    {
        public P(Converter converter) : base(converter)
        {
            Converter.Register("p", this);
        }

        public override string Convert(HtmlNode node)
        {
            var indentation = IndentationFor(node);
            var lineEnd = LineEndFor(node);
            return $"{indentation}{TreatChildren(node).Trim()}{lineEnd}";
        }

        private static string IndentationFor(HtmlNode node)
        {
            if (node.Ancestors("table").Any())
                return string.Empty;

            var length = node.Ancestors("ol").Count() + node.Ancestors("ul").Count();
            bool parentIsList = node.ParentNode.Name.ToLowerInvariant() == "li" || node.ParentNode.Name.ToLowerInvariant() == "ol";
            return parentIsList && node.ParentNode.FirstChild != node
                ? new string(' ', length * 4)
                : Environment.NewLine;
        }

        private static string LineEndFor(HtmlNode node)
        {
            if (node.Ancestors("table").Any())
                return "<br>";

            return Environment.NewLine;
        }
    }
}
