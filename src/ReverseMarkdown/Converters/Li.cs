using System;
using System.IO;
using System.Linq;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Li : ConverterBase {
        public Li(Converter converter) : base(converter)
        {
            Converter.Register("li", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            // Standardize whitespace before inner lists so that the following are equivalent
            //   <li>Foo<ul><li>...
            //   <li>Foo\n    <ul><li>...
            foreach (var innerList in node.SelectNodes("//ul|//ol") ?? Enumerable.Empty<HtmlNode>()) {
                if (innerList.PreviousSibling?.NodeType == HtmlNodeType.Text) {
                    innerList.PreviousSibling.InnerHtml = innerList.PreviousSibling.InnerHtml.Trim(); // TODO optimize
                }
            }

            writer.Write(IndentationFor(node, true));

            if (node.ParentNode is { Name: "ol" }) {
                // index are zero based hence add one
                var index = node.ParentNode.SelectNodes("./li").IndexOf(node) + 1;
                writer.Write(index);
                writer.Write(". ");
            }
            else {
                writer.Write(Converter.Config.ListBulletChar);
                writer.Write(' ');
            }

            var content = ContentFor(node).Trim();
            writer.Write(content);
            writer.WriteLine();
        }

        private string ContentFor(HtmlNode node)
        {
            using var writer = Converter.CreateWriter(node);

            if (Converter.Config.GithubFlavored) {
                if (
                    node.FirstChild is { Name: "input" } childNode &&
                    childNode.GetAttributeValue("type", string.Empty).Equals("checkbox", StringComparison.OrdinalIgnoreCase)
                ) {
                    writer.Write(childNode.Attributes.Contains("checked")
                        ? "[x]"
                        : "[ ]");

                    node.RemoveChild(childNode);
                }
            }

            TreatChildren(writer, node);

            return writer.ToString();
        }
    }
}
