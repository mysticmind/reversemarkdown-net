using System.IO;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class Ol : ConverterBase {
        public Ol(Converter converter) : base(converter)
        {
            Converter.Register("ol", this);
            Converter.Register("ul", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.CommonMark) {
                writer.Write(node.OuterHtml.CompactHtmlForCommonMarkBlock());
                return;
            }

            // Lists inside tables are not supported as markdown, so leave as HTML
            if (Context.AncestorsAny("table")) {
                writer.Write(node.OuterHtml);
                return;
            }

            // Prevent blank lines being inserted in nested lists
            var block = node.ParentNode.Name is not ("ol" or "ul");

            if (block) writer.WriteLine();
            TreatChildren(writer, node);
            if (block) {
                writer.WriteLine();
                if (Converter.Config.CommonMark && NextElementIsList(node)) {
                    writer.WriteLine();
                }
            }
        }

        private static bool NextElementIsList(HtmlNode node)
        {
            var sibling = node.NextSibling;
            while (sibling != null) {
                if (sibling.NodeType == HtmlNodeType.Element) {
                    return sibling.Name is "ul" or "ol";
                }

                sibling = sibling.NextSibling;
            }

            return false;
        }
    }
}
