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
            return $"{indentation}{TreatChildren(node).Trim()}{Environment.NewLine}";
        }

        private static string IndentationFor(HtmlNode node)
        {
            string parentName = node.ParentNode.Name.ToLowerInvariant();

            // If p follows a list item, indent it instead of adding a leading newline
            var length = node.Ancestors("ol").Count() + node.Ancestors("ul").Count();
            bool parentIsList = parentName == "li" || parentName == "ol" || parentName == "ul";
            if (parentIsList && node.ParentNode.FirstChild != node)
                return new string(' ', length * 4);

            // If p is at the start of a table cell, no leading newline
            if ((parentName == "td" || parentName == "th") && node.ParentNode.FirstChild == node)
                return string.Empty;

            return Environment.NewLine;
        }
    }
}
