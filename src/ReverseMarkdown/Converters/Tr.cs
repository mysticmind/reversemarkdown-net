using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using static System.Environment;

namespace ReverseMarkdown.Converters
{
    public class Tr
        : ConverterBase
    {
        public Tr(Converter converter)
            : base(converter)
        {
            Converter.Register("tr", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node).TrimEnd();

            var result = $"|{content}{NewLine}";

            return IsTableHeaderRow(node)
                ? result + UnderlineFor(node)
                : result;
        }

        private static bool IsTableHeaderRow(HtmlNode node)
        {
            return node
                .ChildNodes
                .FindFirst("th") != null;
        }

        private static string UnderlineFor(HtmlNode node)
        {
            // int colCount = node.ChildNodes.Count();

            var cols = new List<string>();

            for (var i = 0; i < node.ChildNodes.Count; i++)
                cols.Add("---");

            return $"| {cols.Aggregate((item1, item2) => $"{item1} | {item2}")} |{NewLine}";
        }
    }
}