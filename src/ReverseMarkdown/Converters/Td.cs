using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Td : ConverterBase {
        public Td(Converter converter) : base(converter)
        {
            Converter.Register("td", this);
            Converter.Register("th", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.SlackFlavored) {
                throw new SlackUnsupportedTagException(node.Name);
            }

            var colSpan = GetColSpan(node);

            var content = TreatChildrenAsString(node)
                .Trim()
                .ReplaceLineEndings("<br>");

            for (var i = 0; i < colSpan; i++) {
                writer.Write(' ');
                writer.Write(content);
                writer.Write(" |");
            }
        }

        /// <summary>
        /// Given node within td tag, checks if newline should be prepended. Will not prepend if this is the first node after any whitespace
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool FirstNodeWithinCell(HtmlNode node)
        {
            var parentName = node.ParentNode.Name;
            // If p is at the start of a table cell, no leading newline
            if (parentName is "td" or "th") {
                var pNodeIndex = node.ParentNode.ChildNodes.GetNodeIndex(node);
                var firstNodeIsWhitespace = node.ParentNode.FirstChild.Name == "#text" && string.IsNullOrWhiteSpace(node.ParentNode.FirstChild.InnerText);
                if (pNodeIndex == 0 || (firstNodeIsWhitespace && pNodeIndex == 1)) return true;
            }

            return false;
        }

        /// <summary>
        /// Given node within td tag, checks if newline should be appended. Will not append if this is the last node before any whitespace
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool LastNodeWithinCell(HtmlNode node)
        {
            var parentName = node.ParentNode.Name;
            if (parentName is "td" or "th") {
                var pNodeIndex = node.ParentNode.ChildNodes.GetNodeIndex(node);
                var cellNodeCount = node.ParentNode.ChildNodes.Count;
                var lastNodeIsWhitespace = node.ParentNode.LastChild.Name == "#text" && string.IsNullOrWhiteSpace(node.ParentNode.LastChild.InnerText);
                if (pNodeIndex == cellNodeCount - 1 || (lastNodeIsWhitespace && pNodeIndex == cellNodeCount - 2)) return true;
            }

            return false;
        }

        private int GetColSpan(HtmlNode node)
        {
            var colSpan = 1;

            if (Converter.Config.TableHeaderColumnSpanHandling && node.Name == "th") {
                colSpan = node.GetAttributeValue("colspan", 1);
            }

            return colSpan;
        }
    }
}
