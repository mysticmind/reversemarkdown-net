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

            if (string.IsNullOrWhiteSpace(content))
            {
                return "";
            }

            // if parent is an ordered or unordered list
            // then table need to be indented as well
            var indent = IndentationFor(node);

            if (IsTableHeaderRow(node) || UseFirstRowAsHeaderRow(node))
            {
                underline = UnderlineFor(node, indent);
            }

            return $"{indent}|{content}{Environment.NewLine}{underline}";
        }

        private bool UseFirstRowAsHeaderRow(HtmlNode node)
        {
            var tableNode = node.Ancestors("table").FirstOrDefault();
            var firstRow = tableNode?.SelectSingleNode(".//tr");

            if (firstRow == null)
            {
                return false;
            }

            var isFirstRow = firstRow == node;
            var hasNoHeaderRow = tableNode.SelectNodes(".//th")?.FirstOrDefault() == null;

            return isFirstRow
                   && hasNoHeaderRow
                   && Converter.Config.TableWithoutHeaderRowHandling ==
                   Config.TableWithoutHeaderRowHandlingOption.Default;
        }

        private static bool IsTableHeaderRow(HtmlNode node)
        {
            return node.ChildNodes.FindFirst("th") != null;
        }

        private static string UnderlineFor(HtmlNode node, string indent)
        {
            var nodes = node.ChildNodes.Where(x => x.Name == "th" || x.Name == "td").ToList();

            var cols = new List<string>();
            foreach (var styles in nodes.Select(nd => StringUtils.ParseStyle(nd.GetAttributeValue("style", ""))))
            {
                styles.TryGetValue("text-align", out var align);

                switch (align)
                {
                    case "left":
                        cols.Add(":---");
                        break;
                    case "right":
                        cols.Add("---:");
                        break;
                    case "center":
                        cols.Add(":---:");
                        break;
                    default:
                        cols.Add("---");
                        break;
                }
            }

            var colsAggregated = string.Join(" | ", cols);

            return $"{indent}| {colsAggregated} |{Environment.NewLine}";
        }
    }
}
