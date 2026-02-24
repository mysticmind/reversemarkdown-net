using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Div : ConverterBase {
        public Div(Converter converter) : base(converter)
        {
            Converter.Register("div", this);
            Converter.Register("header", this);
            Converter.Register("main", this);
            Converter.Register("footer", this);
            Converter.Register("section", this);
            Converter.Register("article", this);
            Converter.Register("nav", this);
            Converter.Register("figure", this);
            Converter.Register("figcaption", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.CommonMark) {
                writer.Write(node.OuterHtml);
                return;
            }

            while (node.ChildNodes.Count == 1 && node.FirstChild.Name == "div") {
                node = node.FirstChild;
            }

            var content = TreatChildrenAsString(node);

            content = Converter.Config.CleanupUnnecessarySpaces
                ? content.Trim()
                : content;

            // if there is a block child then ignore adding the newlines for div
            if (
                node.ChildNodes.Count == 1 &&
                node.FirstChild.Name
                    is "pre"
                    or "p"
                    or "ol"
                    or "oi"
                    or "table"
            ) {
                writer.Write(content);
                return;
            }

            if (
                !Td.FirstNodeWithinCell(node) &&
                !Converter.Config.SuppressDivNewlines
            ) {
                writer.WriteLine();
            }

            writer.Write(content);

            if (!Td.LastNodeWithinCell(node)) {
                writer.WriteLine();
            }
        }
    }
}
