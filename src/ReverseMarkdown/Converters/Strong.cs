using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Strong : ConverterBase {
        public Strong(Converter converter) : base(converter)
        {
            Converter.Register("strong", this);
            Converter.Register("b", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            var isCommonMark = Converter.Config.CommonMark;
            if (isCommonMark && Converter.Config.CommonMarkUseHtmlInlineTags) {
                writer.Write(node.OuterHtml);
                return;
            }

            var content = TreatChildrenAsString(node);

            if (string.IsNullOrEmpty(content) || (!isCommonMark && AlreadyBold())) {
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

            var spaceSuffix = node.NextSibling?.Name is "strong" or "b"
                ? " "
                : "";

            var emphasis = Converter.Config.SlackFlavored
                || Converter.Config.TelegramMarkdownV2
                ? "*"
                : isCommonMark && Context.AncestorsAny("strong")
                    ? "__"
                    : "**";
            var suffix = commonMarkSuffix.Length > 0 || spaceSuffix.Length > 0 ? " " : "";
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

        private bool AlreadyBold()
        {
            return Context.AncestorsAny("strong") || Context.AncestorsAny("b");
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
