using System;
using System.IO;
using System.Linq;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public class A : ConverterBase {
        public A(Converter converter) : base(converter)
        {
            Converter.Register("a", this);
        }

        private readonly StringReplaceValues _escapeValues = new() {
            [" "] = "%20",
            ["("] = "%28",
            [")"] = "%29",
        };

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            var name = TreatChildrenAsString(node).Trim();

            var href = node.GetAttributeValue("href", string.Empty).Trim().Replace(_escapeValues);
            var scheme = StringUtils.GetScheme(href);

            var isRemoveLinkWhenSameName = (
                Converter.Config.SmartHrefHandling &&
                scheme != string.Empty &&
                Uri.IsWellFormedUriString(href, UriKind.RelativeOrAbsolute) && (
                    href.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    href.Equals($"tel:{name}", StringComparison.OrdinalIgnoreCase) ||
                    href.Equals($"mailto:{name}", StringComparison.OrdinalIgnoreCase)
                )
            );

            if (href.StartsWith("#") //anchor link
                || !Converter.Config.IsSchemeWhitelisted(scheme) //Not allowed scheme
                || isRemoveLinkWhenSameName
                || string.IsNullOrEmpty(href) //We would otherwise print empty () here...
               ) {
                writer.Write(name);
                return;
            }

            var useHrefWithHttpWhenNameHasNoScheme = (
                Converter.Config.SmartHrefHandling && (
                    scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                    scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                ) &&
                string.Equals(href, $"{scheme}://{name}", StringComparison.OrdinalIgnoreCase)
            );

            var hasSingleChildImgNode = (
                node.ChildNodes.Count == 1 && // TODO handle whitespace text nodes?
                node.ChildNodes.Count(n => n.Name.Contains("img")) == 1
            );

            // if the anchor tag contains a single child image node don't escape the link text
            var linkText = hasSingleChildImgNode ? name : StringUtils.EscapeLinkText(name);

            if (string.IsNullOrEmpty(linkText)) {
                writer.Write(href);
                return;
            }

            if (useHrefWithHttpWhenNameHasNoScheme) {
                writer.Write(href);
            }
            else {
                writer.Write("[");
                writer.Write(linkText);
                writer.Write("](");
                writer.Write(href);

                if (ExtractTitle(node) is { Length: > 0 } title) {
                    writer.Write(" \"");
                    writer.Write(title);
                    writer.Write("\"");
                }

                writer.Write(")");
            }
        }
    }
}
