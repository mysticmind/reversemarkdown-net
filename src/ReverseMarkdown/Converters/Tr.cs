using System;
using System.Collections.Generic;
using System.Linq;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Tr : ConverterBase
    {
        public Tr(Converter converter) : base(converter)
        {
            Converter.Register("tr", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node).TrimEnd();
            var underline = "";
            
            if (IsTableHeaderRow(node))
            {
                underline = UnderlineFor(node);
            }

            return $"|{content}{Environment.NewLine}{underline}";
        }

        private static bool IsTableHeaderRow(HtmlNode node)
        {
            return node.ChildNodes.FindFirst("th") != null;
        }

        private string UnderlineFor(HtmlNode node)
        {
            var colCount = node.ChildNodes.Count(n => n.Name.Contains("th"));

            var cols = new List<string>();

            for (var i = 0; i < colCount; i++ )
            {
                cols.Add("---");
            }

            var colsAggregated = cols.Aggregate((item1, item2) => item1 + " | " + item2);

            return $"| {colsAggregated} |{Environment.NewLine}";
        }
    }
}
