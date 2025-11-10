using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class P : ConverterBase {
        public P(Converter converter) : base(converter)
        {
            Converter.Register("p", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            TreatIndentation(writer, node);

            var content = TreatChildrenAsString(node);
            if (Converter.Config.CleanupUnnecessarySpaces) {
                content = content.Trim();
            }

            writer.Write(content);

            if (!Td.LastNodeWithinCell(node)) {
                writer.WriteLine();
            }
        }

        private void TreatIndentation(TextWriter writer, HtmlNode node)
        {
            // If p follows a list item, add newline and indent it
            var parentIsList = node.ParentNode.Name is "li" or "ol" or "ul";
            if (parentIsList && node.ParentNode.FirstChild != node) {
                var length = Context.AncestorsCount("ol") + Context.AncestorsCount("ul");
                writer.WriteLine();
                writer.Write(new string(' ', length * 4));
                return;
            }

            // If p is at the start of a table cell, no leading newline
            if (!Td.FirstNodeWithinCell(node)) {
                writer.WriteLine();
            }
        }
    }
}
