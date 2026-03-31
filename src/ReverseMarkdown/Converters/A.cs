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
            var isCommonMark = Converter.Config.CommonMark;
            var isTelegram = Converter.Config.TelegramMarkdownV2;
            var name = TreatChildrenAsString(node);
            if (!isCommonMark && !isTelegram) {
                name = name.Trim();
            }
            else if (isCommonMark) {
                name = name.ReplaceLineEndings("&#10;");
            }

            if (isTelegram) {
                ConvertTelegramMarkdownV2(writer, node, name);
                return;
            }

            if (isCommonMark &&
                node.FirstChild?.NodeType == HtmlNodeType.Text &&
                (node.InnerText.Contains("\\", StringComparison.Ordinal) ||
                 node.InnerText.Contains("<", StringComparison.Ordinal) ||
                 node.InnerText.Contains(">", StringComparison.Ordinal))) {
                WriteRawHtmlAnchor(writer, node, EncodeAnchorText(node.InnerText));
                return;
            }



            if (isCommonMark && (name.Contains('[') || name.Contains(']') || name.Contains('\n'))) {
                writer.Write(node.OuterHtml);
                return;
            }

            var hrefAttribute = node.Attributes["href"];
            var hasHrefAttribute = hrefAttribute != null;
            var href = node.GetAttributeValue("href", string.Empty).Trim();
            if (!isCommonMark) {
                href = href.Replace(_escapeValues);
            }
            else {
                href = href.Replace("\\", "\\\\");
                href = href.Replace("*", "\\*").Replace("_", "\\_");
                if (href.Contains('\n') || href.Contains('\r')) {
                    writer.Write(node.OuterHtml);
                    return;
                }
                var openCount = href.Count(c => c == '(');
                var closeCount = href.Count(c => c == ')');
                if (closeCount > openCount) {
                    href = href.Replace(")", "\\)");
                }
                if (href.Contains(' ') || href.Contains('(') || href.Contains(')')) {
                    href = $"<{href.Replace("<", "%3C").Replace(">", "%3E")}>";
                }
            }

            if (isCommonMark && !hasHrefAttribute) {
                writer.Write(node.OuterHtml);
                return;
            }

            if (isCommonMark &&
                node.OuterHtml.IndexOf("href=", StringComparison.OrdinalIgnoreCase) < 0) {
                writer.Write(node.OuterHtml);
                return;
            }

            if (isCommonMark && hrefAttribute != null) {
                if (hrefAttribute.Value.Contains("&", StringComparison.Ordinal) ||
                    hrefAttribute.Value.Contains("\\", StringComparison.Ordinal)) {
                    writer.Write(node.OuterHtml);
                    return;
                }

                var outerHtml = node.OuterHtml;
                if (outerHtml.Contains("href=\"&", StringComparison.OrdinalIgnoreCase) ||
                    outerHtml.Contains("href='&", StringComparison.OrdinalIgnoreCase) ||
                    outerHtml.Contains("href=\"\\", StringComparison.OrdinalIgnoreCase) ||
                    outerHtml.Contains("href='\\", StringComparison.OrdinalIgnoreCase)) {
                    writer.Write(outerHtml);
                    return;
                }

                var text = node.InnerText;
                if (text.Contains("\\", StringComparison.Ordinal) ||
                    text.Contains("[", StringComparison.Ordinal) ||
                    text.Contains("]", StringComparison.Ordinal) ||
                    text.Contains("<", StringComparison.Ordinal) ||
                    text.Contains("*", StringComparison.Ordinal) ||
                    text.Contains("_", StringComparison.Ordinal)) {
                    writer.Write(node.OuterHtml);
                    return;
                }
            }

            if (isCommonMark && string.IsNullOrEmpty(name)) {
                if (!hasHrefAttribute) {
                    writer.Write(node.OuterHtml);
                }
                else if (string.IsNullOrEmpty(href)) {
                    writer.Write("[]()");
                }
                else {
                    writer.Write("[](");
                    writer.Write(href);
                    writer.Write(")");
                }

                return;
            }

            if (isCommonMark && hrefAttribute != null &&
                hrefAttribute.Value.Contains("&", StringComparison.Ordinal)) {
                writer.Write(node.OuterHtml);
                return;
            }

            if (isCommonMark && href.Contains('`')) {
                writer.Write(node.OuterHtml);
                return;
            }
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

            if ((!Converter.Config.CommonMark && href.StartsWith("#")) //anchor link
                || !Converter.Config.IsSchemeWhitelisted(scheme) //Not allowed scheme
                || isRemoveLinkWhenSameName
                || (string.IsNullOrEmpty(href) && !Converter.Config.CommonMark) //We would otherwise print empty () here...
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
                if (Converter.Config.CommonMark && string.IsNullOrEmpty(href)) {
                    writer.Write("[]()");
                }
                else {
                    writer.Write(href);
                }

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

        private void ConvertTelegramMarkdownV2(TextWriter writer, HtmlNode node, string name)
        {
            var href = node.GetAttributeValue("href", string.Empty).Trim();
            var hasHrefAttribute = node.Attributes["href"] != null;
            var escapedName = StringUtils.EscapeTelegramMarkdownV2(name);

            if (!hasHrefAttribute) {
                writer.Write(escapedName);
                return;
            }

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

            if (href.StartsWith("#", StringComparison.Ordinal)
                || !Converter.Config.IsSchemeWhitelisted(scheme)
                || isRemoveLinkWhenSameName
                || string.IsNullOrEmpty(href)) {
                writer.Write(escapedName);
                return;
            }

            var escapedHref = StringUtils.EscapeTelegramMarkdownV2LinkUrl(href);
            writer.Write('[');
            writer.Write(escapedName);
            writer.Write("](");
            writer.Write(escapedHref);
            writer.Write(')');
        }

        private static void WriteRawHtmlAnchor(TextWriter writer, HtmlNode node, string text)
        {
            writer.Write("<a");
            foreach (var attribute in node.Attributes) {
                writer.Write(' ');
                writer.Write(attribute.Name);
                writer.Write("=\"");
                writer.Write(attribute.Value);
                writer.Write('"');
            }

            writer.Write('>');
            writer.Write(text);
            writer.Write("</a>");
        }

        private static string EncodeAnchorText(string text)
        {
            return text.Replace("\\", "&#92;").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
