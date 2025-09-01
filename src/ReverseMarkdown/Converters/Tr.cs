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
            if (Converter.Config.SlackFlavored)
            {
                throw new SlackUnsupportedTagException(node.Name);
            }
            
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
                underline = UnderlineFor(node, indent, Converter.Config.TableHeaderColumnSpanHandling);
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
            var tableNode = node.Ancestors("table").FirstOrDefault();
            var firstRow = tableNode?.SelectSingleNode(".//tr");
            if (firstRow != null && firstRow == node)
            {
                return node.ChildNodes.FindFirst("th") != null;
            }

            return false;
        }

        private static string UnderlineFor(HtmlNode node, string indent, bool tableHeaderColumnSpanHandling)
        {
            var nodes = node.ChildNodes.Where(x => x.Name == "th" || x.Name == "td").ToList();

            var cols = new List<string>();
            foreach (var nd in nodes)
            {
                var colSpan = GetColSpan(nd, tableHeaderColumnSpanHandling);
                var styles = StringUtils.ParseStyle(nd.GetAttributeValue("style", ""));
                styles.TryGetValue("text-align", out var align);

                string content;
                switch (align?.Trim())
                {
                    case "left":
                        content = ":---";
                        break;
                    case "right":
                        content ="---:";
                        break;
                    case "center":
                        content = ":---:";
                        break;
                    default:
                        content ="---";
                        break;
                }

                for (var i = 0; i < colSpan; i++) {
                    cols.Add(content);
                }
            }

            var colsAggregated = string.Join(" | ", cols);

            return $"{indent}| {colsAggregated} |{Environment.NewLine}";
        }
        
        private static int GetColSpan(HtmlNode node, bool tableHeaderColumnSpanHandling)
        {
            var colSpan = 1;
            
            if (tableHeaderColumnSpanHandling && node.Name == "th")
            {
                colSpan = node.GetAttributeValue("colspan", 1);
            }
            return colSpan;
        }
    }
}
