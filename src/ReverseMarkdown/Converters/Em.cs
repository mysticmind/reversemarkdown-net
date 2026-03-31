using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Em : ConverterBase {
        public Em(Converter converter) : base(converter)
        {
            Converter.Register("em", this);
            Converter.Register("i", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            var isCommonMark = Converter.Config.CommonMark;
            if (isCommonMark && Converter.Config.CommonMarkUseHtmlInlineTags) {
                var innerHtml = node.InnerHtml;
                if (innerHtml.Contains('[') || innerHtml.Contains(']')) {
                    writer.Write('<');
                    writer.Write(node.Name);
                    foreach (var attribute in node.Attributes) {
                        writer.Write(' ');
                        writer.Write(attribute.Name);
                        writer.Write("=\"");
                        writer.Write(attribute.Value);
                        writer.Write('"');
                    }

                    writer.Write('>');
                    writer.Write(innerHtml.Replace("[", "&#91;").Replace("]", "&#93;"));
                    writer.Write("</");
                    writer.Write(node.Name);
                    writer.Write('>');
                }
                else {
                    writer.Write(node.OuterHtml);
                }

                return;
            }

            var content = TreatChildrenAsString(node);

            if (string.IsNullOrWhiteSpace(content) || (!isCommonMark && AlreadyItalic())) {
                writer.Write(content);
                return;
            }

            var commonMarkPrefix = string.Empty;
            var commonMarkSuffix = string.Empty;
            if (isCommonMark && Converter.Config.CommonMarkIntrawordEmphasisSpacing) {
                var contentFirst = FirstNonWhitespaceChar(content);
                var contentLast = LastNonWhitespaceChar(content);
                var contentHasLeadingWhitespace = content.Length > 0 && char.IsWhiteSpace(content[0]);
                var contentHasTrailingWhitespace = content.Length > 0 && char.IsWhiteSpace(content[content.Length - 1]);

                if (IsWordChar(contentFirst) && IsWordChar(contentLast)) {
                    var hasPrevWord = !contentHasLeadingWhitespace &&
                                      IsAdjacentWordChar(node.PreviousSibling, checkEnd: true);
                    var hasNextWord = !contentHasTrailingWhitespace &&
                                      IsAdjacentWordChar(node.NextSibling, checkEnd: false);
                    if (hasPrevWord && hasNextWord) {
                        commonMarkPrefix = " ";
                        commonMarkSuffix = " ";
                    }
                }
            }

            var spaceSuffix = node.NextSibling?.Name is "i" or "em"
                ? " "
                : string.Empty;

            var emphasis = Converter.Config.SlackFlavored
                || Converter.Config.TelegramMarkdownV2
                ? "_"
                : isCommonMark && Context.AncestorsAny("i")
                    ? "_"
                    : "*";
            var suffix = commonMarkSuffix.Length > 0 || spaceSuffix.Length > 0 ? " " : string.Empty;
            if (commonMarkPrefix.Length > 0) {
                writer.Write(commonMarkPrefix);
            }

            TreatEmphasizeContentWhitespaceGuard(
                writer,
                content,
                emphasis,
                suffix,
                preserveLineEndings: isCommonMark
            );
        }

        private bool AlreadyItalic()
        {
            return Context.AncestorsAny("i") || Context.AncestorsAny("em");
        }

        private static bool IsWordChar(char? value)
        {
            return value.HasValue && char.IsLetterOrDigit(value.Value);
        }

        private static char? FirstNonWhitespaceChar(string content)
        {
            foreach (var c in content) {
                if (!char.IsWhiteSpace(c)) {
                    return c;
                }
            }

            return null;
        }

        private static char? LastNonWhitespaceChar(string content)
        {
            for (var i = content.Length - 1; i >= 0; i--) {
                var c = content[i];
                if (!char.IsWhiteSpace(c)) {
                    return c;
                }
            }

            return null;
        }

        private static bool IsAdjacentWordChar(HtmlNode? sibling, bool checkEnd)
        {
            if (sibling == null) {
                return false;
            }

            var text = sibling.InnerText;
            if (string.IsNullOrEmpty(text)) {
                return false;
            }

            var index = checkEnd ? text.Length - 1 : 0;
            var c = text[index];
            if (char.IsWhiteSpace(c)) {
                return false;
            }

            return char.IsLetterOrDigit(c);
        }
    }
}
