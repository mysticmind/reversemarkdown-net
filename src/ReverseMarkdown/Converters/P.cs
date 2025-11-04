using System;
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
            var newlineAfter = NewlineAfter(node);

            var content = TreatChildren(node);
            if (Converter.Config.CleanupUnnecessarySpaces)
            {
                content = content.Trim();
            }

            return $"{indentation}{content}{newlineAfter}";
        }

        private string IndentationFor(HtmlNode node)
        {
            string parentName = node.ParentNode.Name.ToLowerInvariant();

            // If p follows a list item, add newline and indent it
            bool parentIsList = parentName is "li" or "ol" or "ul";
            if (parentIsList && node.ParentNode.FirstChild != node) {
                var length = Context.AncestorsCount("ol") + Context.AncestorsCount("ul");
                return Environment.NewLine + (new string(' ', length * 4));
            }

            // If p is at the start of a table cell, no leading newline
            return Td.FirstNodeWithinCell(node) ? string.Empty : Environment.NewLine;
        }

        private static string NewlineAfter(HtmlNode node)
        {
            return Td.LastNodeWithinCell(node) ? string.Empty : Environment.NewLine;
        }
    }
}