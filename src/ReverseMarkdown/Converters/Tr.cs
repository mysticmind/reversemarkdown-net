using System.IO;
using System.Linq;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class Tr : ConverterBase {
        public Tr(Converter converter) : base(converter)
        {
            Converter.Register("tr", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.SlackFlavored) {
                throw new SlackUnsupportedTagException(node.Name);
            }

            var content = TreatChildrenAsString(node).TrimEnd();

            if (string.IsNullOrWhiteSpace(content)) {
                return;
            }

            // if parent is an ordered or unordered list
            // then table need to be indented as well
            var indent = IndentationFor(node);

            writer.Write(indent);
            writer.Write('|');
            writer.Write(content);
            writer.WriteLine();

            if (ShouldWriteUnderline(node)) {
                writer.Write(indent);
                WriteUnderline(writer, node, Converter.Config.TableHeaderColumnSpanHandling);
            }
        }


        private bool ShouldWriteUnderline(HtmlNode node)
        {
            var tableNode = node.Ancestors("table").FirstOrDefault();
            var firstRow = tableNode?.SelectSingleNode(".//tr");

            if (firstRow == null || tableNode == null) {
                return false;
            }

            var isFirstRow = firstRow == node;

            return (
                isFirstRow && (
                    IsTableHeaderRow(node) ||
                    UseFirstRowAsHeaderRow(node)
                )
            );

            bool UseFirstRowAsHeaderRow(HtmlNode node)
            {
                var hasNoHeaderRow = tableNode.SelectNodes(".//th")?.FirstOrDefault() == null;
                return hasNoHeaderRow
                       && Converter.Config.TableWithoutHeaderRowHandling ==
                       Config.TableWithoutHeaderRowHandlingOption.Default;
            }

            static bool IsTableHeaderRow(HtmlNode node)
            {
                return node.ChildNodes.FindFirst("th") != null!;
            }
        }


        private static void WriteUnderline(TextWriter writer, HtmlNode node, bool tableHeaderColumnSpanHandling)
        {
            var nodes = node.ChildNodes.Where(x => x.Name is "th" or "td");
            foreach (var nd in nodes) {
                var colSpan = GetColSpan(nd, tableHeaderColumnSpanHandling);
                var styles = StringUtils.ParseStyle(nd.GetAttributeValue("style", string.Empty));
                styles.TryGetValue("text-align", out var align);

                var content = align switch {
                    "left" => ":---",
                    "right" => "---:",
                    "center" => ":---:",
                    _ => "---"
                };

                for (var i = 0; i < colSpan; i++) {
                    writer.Write('|');
                    writer.Write(' ');
                    writer.Write(content);
                    writer.Write(' ');
                }
            }

            writer.WriteLine('|');
        }


        private static int GetColSpan(HtmlNode node, bool tableHeaderColumnSpanHandling)
        {
            var colSpan = 1;

            if (tableHeaderColumnSpanHandling && node.Name is "th") {
                colSpan = node.GetAttributeValue("colspan", 1);
            }

            return colSpan;
        }
    }
}
