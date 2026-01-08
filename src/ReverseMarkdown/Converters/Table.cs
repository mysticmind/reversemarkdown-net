using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class Table : ConverterBase {
        public Table(Converter converter) : base(converter)
        {
            Converter.Register("table", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.SlackFlavored) {
                throw new SlackUnsupportedTagException(node.Name);
            }

            // Tables inside tables are not supported as markdown, so leave as HTML
            if (Context.AncestorsAny("table")) {
                // Compact the nested table HTML to prevent breaking the markdown table
                writer.Write(node.OuterHtml.CompactHtmlForMarkdown());
                return;
            }
            
            var captionNode = node.SelectSingleNode("caption");
            var captionText = captionNode?.InnerText?.Trim();
            captionNode?.Remove();

            // if table does not have a header row , add empty header row if set in config
            var useEmptyRowForHeader = (
                this.Converter.Config.TableWithoutHeaderRowHandling == Config.TableWithoutHeaderRowHandlingOption.EmptyRow
            );

            var emptyHeaderRow = HasNoTableHeaderRow(node) && useEmptyRowForHeader
                ? EmptyHeader(node)
                : string.Empty;
            
            // add caption text as a paragraph above table
            if (captionText != string.Empty)
            {
                writer.WriteLine();
                writer.WriteLine();
                writer.WriteLine(captionText);
            }

            writer.WriteLine();
            writer.WriteLine();

            writer.Write(emptyHeaderRow);
            TreatChildren(writer, node);
            writer.WriteLine();
        }

        private static bool HasNoTableHeaderRow(HtmlNode node)
        {
            var thNode = node.SelectNodes("//th")?.FirstOrDefault();
            return thNode == null;
        }

        private static string EmptyHeader(HtmlNode node)
        {
            var firstRow = node.SelectNodes("//tr")?.FirstOrDefault();

            if (firstRow == null) {
                return string.Empty;
            }

            var colCount = firstRow.ChildNodes.Count(n => n.Name.Contains("td") || n.Name.Contains("th"));

            var headerRowItems = new List<string>();
            var underlineRowItems = new List<string>();

            for (var i = 0; i < colCount; i++) {
                headerRowItems.Add("<!---->");
                underlineRowItems.Add("---");
            }

            var headerRow = $"| {string.Join(" | ", headerRowItems)} |{Environment.NewLine}";
            var underlineRow = $"| {string.Join(" | ", underlineRowItems)} |{Environment.NewLine}";

            return headerRow + underlineRow;
        }
    }
}
