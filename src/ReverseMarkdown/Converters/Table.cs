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
            if (Converter.Config.CommonMark) {
                writer.Write(node.OuterHtml);
                return;
            }

            if (Converter.Config.SlackFlavored) {
                throw new SlackUnsupportedTagException(node.Name);
            }

            if (Converter.Config.TelegramMarkdownV2) {
                WriteTelegramFallback(writer, node);
                return;
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

        private static void WriteTelegramFallback(TextWriter writer, HtmlNode node)
        {
            var captionText = node.SelectSingleNode("caption")?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(captionText)) {
                writer.WriteLine();
                writer.WriteLine(StringUtils.EscapeTelegramMarkdownV2(captionText));
            }

            var rows = node.SelectNodes(".//tr");
            if (rows == null || rows.Count == 0) {
                var plainText = HtmlEntity.DeEntitize(node.InnerText).Trim();
                if (!string.IsNullOrEmpty(plainText)) {
                    writer.Write(StringUtils.EscapeTelegramMarkdownV2(plainText));
                }

                return;
            }

            var renderedRows = new List<string>(rows.Count);
            foreach (var row in rows) {
                var cells = row.SelectNodes("./th|./td");
                if (cells == null || cells.Count == 0) {
                    var rowText = NormalizeWhitespace(row.InnerText);
                    if (!string.IsNullOrEmpty(rowText)) {
                        renderedRows.Add(rowText);
                    }

                    continue;
                }

                var cellTexts = cells
                    .Select(cell => NormalizeWhitespace(cell.InnerText))
                    .ToArray();
                renderedRows.Add(string.Join(" | ", cellTexts));
            }

            if (renderedRows.Count == 0) {
                return;
            }

            writer.WriteLine();
            writer.WriteLine("```");
            foreach (var row in renderedRows) {
                writer.WriteLine(StringUtils.EscapeTelegramMarkdownV2Code(row));
            }

            writer.Write("```");
            writer.WriteLine();
        }

        private static string NormalizeWhitespace(string value)
        {
            var decoded = HtmlEntity.DeEntitize(value);
            return string.Join(" ", decoded.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
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
