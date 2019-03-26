using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            
            if (IsTableHeaderRow(node) || UseFirstRowAsHeaderRow(node))
            {
                underline = UnderlineFor(node);
            }

            return $"|{content}{Environment.NewLine}{underline}";
        }

        private bool UseFirstRowAsHeaderRow(HtmlNode node)
        {
            var tableNode = node.ParentNode;
            var firstRow = tableNode.SelectNodes("tr")?.FirstOrDefault();

            if (firstRow == null)
            {
                return false;
            }

            var isFirstRow = firstRow == node;
            var hasNoHeaderRow = tableNode.SelectNodes("//th")?.FirstOrDefault() == null;

            return isFirstRow
                   && hasNoHeaderRow
                   && Converter.Config.TableWithoutHeaderRowHandling ==
                   Config.TableWithoutHeaderRowHandlingOption.Default;
        }

        private static bool IsTableHeaderRow(HtmlNode node)
        {
            return node.ChildNodes.FindFirst("th") != null;
        }

        private string UnderlineFor(HtmlNode node)
        {
            var colCount = node.ChildNodes.Count;

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
