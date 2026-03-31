using System;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


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

            var baseIndentation = IndentationFor(node, true);
            writer.Write(baseIndentation);

            if (node.ParentNode is { Name: "ol" }) {
                // index are zero based hence add one
                var start = node.ParentNode.GetAttributeValue("start", 1);
                var index = node.ParentNode.SelectNodes("./li").IndexOf(node) + start;
                writer.Write(index);
                writer.Write(Converter.Config.TelegramMarkdownV2 ? "\\. " : ". ");
            }
            else {
                if (Converter.Config.TelegramMarkdownV2) {
                    writer.Write(StringUtils.EscapeTelegramMarkdownV2(Converter.Config.ListBulletChar.ToString()));
                }
                else {
                    writer.Write(Converter.Config.ListBulletChar);
                }

                writer.Write(' ');
            }

            var content = ContentFor(node);

            if (!Converter.Config.CommonMark) {
                content = content.Trim();
                writer.Write(content);
                writer.WriteLine();
                return;
            }

            var markerLength = node.ParentNode is { Name: "ol" }
                ? ($"{node.ParentNode.GetAttributeValue("start", 1) + node.ParentNode.SelectNodes("./li").IndexOf(node)}. ").Length
                : 2;

            var indentation = baseIndentation + new string(' ', markerLength);
            var lines = content.ReplaceLineEndings("\n").Split('\n');

            if (lines.Length == 0) {
                writer.WriteLine();
                return;
            }

            writer.WriteLine(lines[0].TrimEnd());

            for (var i = 1; i < lines.Length; i++) {
                var line = lines[i];
                if (line.Length == 0) {
                    writer.WriteLine();
                    continue;
                }

                if (line[0] == ' ' || line[0] == '\t') {
                    writer.WriteLine(line);
                    continue;
                }

                writer.Write(indentation);
                writer.WriteLine(line);
            }
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
