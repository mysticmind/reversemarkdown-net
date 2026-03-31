using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public partial class Text : ConverterBase {
        public Text(Converter converter) : base(converter)
        {
            Converter.Register("#text", this);
        }


        #region values

        private static readonly StringReplaceValues _escapedKeyChars = new() {
            ["*"] = @"\*",
            ["_"] = @"\_",
        };

        private static readonly StringReplaceValues _escapedKeyCharsReverse = new() {
            [@"\*"] = "*",
            [@"\_"] = "_",
        };

        private static readonly StringReplaceValues _preserveAngleBrackets = new() {
            ["&lt;"] = "%3C",
            ["&gt;"] = "%3E",
        };

        private static readonly StringReplaceValues _unPreserveAngleBrackets = new() {
            ["%3C"] = "&lt;",
            ["%3E"] = "&gt;",
        };

        [GeneratedRegex(@"`.*?`")]
        private static partial Regex BackTicks();

        [GeneratedRegex(@"!?\[[^\]\r\n]*\]\([^\)\r\n]*\)")]
        private static partial Regex CommonMarkInlineLinkOrImagePattern();

        [GeneratedRegex(@"\[[^\]\r\n]+\]\[[^\]\r\n]*\]")]
        private static partial Regex CommonMarkReferenceLinkPattern();

        [GeneratedRegex(@"(?m)^ {0,3}\[[^\]\r\n]+\]:")]
        private static partial Regex CommonMarkLinkDefinitionPattern();

        #endregion


        public override void Convert(TextWriter writer, HtmlNode node)
        {
            var isCommonMark = Converter.Config.CommonMark;
            var innerText = node.InnerText;
            if (innerText is " " or "&nbsp;" || innerText == "\u00A0") {
                if (node.ParentNode.Name is not ("ol" or "ul")) {
                    if (isCommonMark && innerText != " ") {
                        writer.Write("&nbsp;");
                    }
                    else {
                        writer.Write(' ');
                    }

                    return;
                }
            }

            if (isCommonMark) {
                if (innerText == "!" && node.NextSibling?.Name == "a") {
                    writer.Write("\\!");
                    return;
                }

                if (innerText == "*" && node.NextSibling?.Name == "img") {
                    writer.Write("\\*");
                    return;
                }
            }

            TreatText(writer, node);
        }


        private void TreatText(TextWriter writer, HtmlNode node)
        {
            var isCommonMark = Converter.Config.CommonMark;
            var isTelegram = Converter.Config.TelegramMarkdownV2;
            var rawText = isCommonMark
                ? node.OuterHtml
                : node.InnerText;
            if (isCommonMark &&
                (rawText.Contains("<!--", StringComparison.Ordinal) ||
                 rawText.Contains("<![CDATA[", StringComparison.Ordinal) ||
                 rawText.Contains("</", StringComparison.Ordinal) ||
                 rawText.Contains("<!", StringComparison.Ordinal))) {
                writer.Write(EscapeSpecialCommonMarkCharacters(rawText));
                return;
            }
            var text = isCommonMark
                ? PreserveCommonMarkAmpersands(rawText)
                : rawText;
            var hasLeadingNbsp = isCommonMark &&
                                 System.Text.RegularExpressions.Regex.IsMatch(
                                     rawText,
                                     @"^\s*(&nbsp;|&#160;)",
                                     System.Text.RegularExpressions.RegexOptions.IgnoreCase
                                 );
            var parent = node.ParentNode;

            if (string.IsNullOrEmpty(text)) {
                return;
            }

            //strip leading spaces and tabs for text within a list item
            var shouldTrim = (
                parent.Name is "table" or "thead" or "tbody" or "ol" or "ul" or "th" or "tr"
            );
            var replaceLineEndings = (
                parent.Name is "p" or "#document" &&
                //(Context.AncestorsAny("th") || Context.AncestorsAny("td"))
                (parent.Ancestors("th").Any() || parent.Ancestors("td").Any())
            );

            // Prevent &lt; and &gt; from being converted to < and > as this will be interpreted as HTML by Markdown
            //var search = SearchValues.Create(["&lt;", "&gt;"], StringComparison.Ordinal);
            //var index = text.IndexOfAny(search);
            //if (index != -1) {
            //}

            // html decode:
            var content = BackTicks().Replace(text, p => DecodeHtml(p.Value));
            content = content.Replace(_preserveAngleBrackets);
            content = DecodeHtml(content);
            content = content.Replace(_unPreserveAngleBrackets);

            if (isCommonMark) {
                content = EscapeCommonMarkBackslashes(content);
                content = RestoreCommonMarkAmpersands(content);
                content = content.Replace("\u00A0", "&nbsp;");
                content = content.Replace("\t", "&#9;");
                if (hasLeadingNbsp && !content.StartsWith("&nbsp;")) {
                    content = "&nbsp;" + content.TrimStart();
                }
                if (!content.Contains("<!--", StringComparison.Ordinal) &&
                    !content.Contains("<![CDATA[", StringComparison.Ordinal) &&
                    !content.Contains("<!", StringComparison.Ordinal) &&
                    !content.Contains("</", StringComparison.Ordinal)) {
                    content = content.Replace("<", "&lt;").Replace(">", "&gt;");
                }
            }

            if (isTelegram && parent.Name != "a") {
                content = StringUtils.EscapeTelegramMarkdownV2(content);
            }

            if (shouldTrim) {
                content = content.Trim();
            }

            if (Converter.Config.CommonMark) {
                content = Regex.Replace(content, "\r?\n\r?\n", "&#10;&#10;");
            }

            if (replaceLineEndings) {
                content = content.ReplaceLineEndings("<br>");
            }

            if (Converter.Config.CommonMark && node.PreviousSibling?.Name == "br") {
                content = content.TrimStart('\r', '\n');
            }

            if (!isTelegram && parent.Name != "a" && !Converter.Config.SlackFlavored) {
                content = content.Replace(_escapedKeyChars);
                // Preserve Key Chars Within BackTicks:
                content = BackTicks().Replace(content, p => p.Value.Replace(_escapedKeyCharsReverse));
            }

            if (isCommonMark) {
                content = EscapeSpecialCommonMarkCharacters(content, node);
                content = content.Replace("`", "\\`");
            }

            if (isCommonMark || Converter.Config.EscapeMarkdownLineStarts) {
                content = EscapeMarkdownLineStarts(content);
            }

            writer.Write(content);
        }


        private const string AmpersandPlaceholder = "__REVERSEMARKDOWN_AMP__";
        private const string NbspPlaceholder = "__REVERSEMARKDOWN_NBSP__";

        private static string EscapeSpecialCommonMarkCharacters(string content, HtmlNode node)
        {
            var escaped = EscapeSpecialCommonMarkCharacters(content);
            return TryGetMarkedDelimiterSequence(node, out var delimiterMarks)
                ? EscapeMarkedDelimiters(escaped, delimiterMarks)
                : escaped;
        }

        private static string EscapeSpecialCommonMarkCharacters(string content)
        {
            return content.StartsWith('`') && content.EndsWith('`')
                ? content
                : EscapeCommonMarkPatternDelimiters(content);
        }

        private static bool TryGetMarkedDelimiterSequence(HtmlNode node, out bool[] delimiterMarks)
        {
            delimiterMarks = Array.Empty<bool>();

            var parent = node.ParentNode;
            if (parent == null || parent.ChildNodes.Count < 2) {
                return false;
            }

            if (!parent.ChildNodes.Any(child => child.NodeType != HtmlNodeType.Text)) {
                return false;
            }

            var parentText = new StringBuilder();
            var nodeInnerText = node.InnerText;
            var nodeStart = -1;

            foreach (var child in parent.ChildNodes) {
                if (child == node) {
                    nodeStart = parentText.Length;
                }

                parentText.Append(child.InnerText);
            }

            if (nodeStart < 0 || string.IsNullOrEmpty(nodeInnerText)) {
                return false;
            }

            var combined = parentText.ToString();
            var shouldEscape = new bool[combined.Length];
            var hasDelimitersToEscape =
                MarkCommonMarkPatternDelimiters(shouldEscape, combined, CommonMarkInlineLinkOrImagePattern()) |
                MarkCommonMarkPatternDelimiters(shouldEscape, combined, CommonMarkReferenceLinkPattern()) |
                MarkCommonMarkPatternDelimiters(shouldEscape, combined, CommonMarkLinkDefinitionPattern());

            if (!hasDelimitersToEscape) {
                return false;
            }

            var marks = new bool[nodeInnerText.Count(IsCommonMarkDelimiter)];
            var delimiterIndex = 0;
            var hasMarkedDelimiterInNode = false;
            for (var i = 0; i < nodeInnerText.Length; i++) {
                var currentChar = nodeInnerText[i];
                if (!IsCommonMarkDelimiter(currentChar)) {
                    continue;
                }

                var marked = shouldEscape[nodeStart + i];
                marks[delimiterIndex++] = marked;
                hasMarkedDelimiterInNode |= marked;
            }

            if (!hasMarkedDelimiterInNode) {
                return false;
            }

            delimiterMarks = marks;
            return true;
        }

        private static string EscapeMarkedDelimiters(string content, bool[] delimiterMarks)
        {
            if (string.IsNullOrEmpty(content) || delimiterMarks.Length == 0) {
                return content;
            }

            var escaped = new StringBuilder(content.Length);
            var delimiterIndex = 0;
            for (var i = 0; i < content.Length; i++) {
                var currentChar = content[i];
                if (!IsCommonMarkDelimiter(currentChar)) {
                    escaped.Append(currentChar);
                    continue;
                }

                var shouldEscape = delimiterIndex < delimiterMarks.Length && delimiterMarks[delimiterIndex];
                delimiterIndex++;
                if (shouldEscape && (i == 0 || content[i - 1] != '\\')) {
                    escaped.Append('\\');
                }

                escaped.Append(currentChar);
            }

            return escaped.ToString();
        }

        private static string EscapeCommonMarkPatternDelimiters(string content)
        {
            if (string.IsNullOrEmpty(content)) {
                return content;
            }

            var shouldEscape = new bool[content.Length];
            var hasDelimitersToEscape =
                MarkCommonMarkPatternDelimiters(shouldEscape, content, CommonMarkInlineLinkOrImagePattern()) |
                MarkCommonMarkPatternDelimiters(shouldEscape, content, CommonMarkReferenceLinkPattern()) |
                MarkCommonMarkPatternDelimiters(shouldEscape, content, CommonMarkLinkDefinitionPattern());

            if (!hasDelimitersToEscape) {
                return content;
            }

            var escaped = new StringBuilder(content.Length);
            for (var i = 0; i < content.Length; i++) {
                if (shouldEscape[i] && (i == 0 || content[i - 1] != '\\')) {
                    escaped.Append('\\');
                }

                escaped.Append(content[i]);
            }

            return escaped.ToString();
        }

        private static bool MarkCommonMarkPatternDelimiters(bool[] shouldEscape, string content, Regex pattern)
        {
            var foundDelimiters = false;

            foreach (Match match in pattern.Matches(content)) {
                var end = match.Index + match.Length;
                for (var i = match.Index; i < end; i++) {
                    if (IsCommonMarkDelimiter(content[i])) {
                        shouldEscape[i] = true;
                        foundDelimiters = true;
                    }
                }
            }

            return foundDelimiters;
        }

        private static bool IsCommonMarkDelimiter(char character)
        {
            return character is '[' or ']' or '(' or ')' or '{' or '}';
        }

        private static string PreserveCommonMarkAmpersands(string rawContent)
        {
            if (string.IsNullOrEmpty(rawContent)) {
                return rawContent;
            }

            var preserved = Regex.Replace(rawContent, "&amp;", AmpersandPlaceholder, RegexOptions.IgnoreCase);
            preserved = Regex.Replace(preserved, "&nbsp;", NbspPlaceholder, RegexOptions.IgnoreCase);
            return preserved;
        }

        private static string RestoreCommonMarkAmpersands(string content)
        {
            if (string.IsNullOrEmpty(content)) {
                return content;
            }

            var restored = Regex.Replace(
                content,
                AmpersandPlaceholder + @"(#[0-9]+|#x[0-9a-fA-F]+|[A-Za-z][A-Za-z0-9]+);",
                "\\&$1;"
            );

            restored = restored.Replace(NbspPlaceholder, "&nbsp;");
            return restored.Replace(AmpersandPlaceholder, "&");
        }

        private static string EscapeCommonMarkBackslashes(string content)
        {
            if (string.IsNullOrEmpty(content)) {
                return content;
            }

            return content.Replace("\\", "\\\\");
        }

        private static string EscapeMarkdownLineStarts(string content)
        {
            if (string.IsNullOrEmpty(content)) {
                return content;
            }

            var normalized = content.ReplaceLineEndings("\n");
            var lines = normalized.Split('\n');
            for (var i = 0; i < lines.Length; i++) {
                lines[i] = EscapeLineStart(lines[i]);
            }

            return string.Join("\n", lines);
        }

        private static string EscapeLineStart(string line)
        {
            if (string.IsNullOrEmpty(line)) {
                return line;
            }

            var index = 0;
            var maxIndent = 3;
            while (index < line.Length && line[index] == ' ' && index < maxIndent) {
                index++;
            }

            if (index >= line.Length || line[index] == '\\') {
                return line;
            }

            var current = line[index];
            if (IsSetextUnderline(line, index)) {
                return line.Insert(index, "\\");
            }

            if (current == '#') {
                return line.Insert(index, "\\");
            }

            if ((current == '-' || current == '*' || current == '+') && IsLineMarker(line, index, 1)) {
                return line.Insert(index, "\\");
            }

            if (char.IsDigit(current)) {
                var digitEnd = index;
                while (digitEnd < line.Length && char.IsDigit(line[digitEnd])) {
                    digitEnd++;
                }

                if (digitEnd < line.Length && (line[digitEnd] == '.' || line[digitEnd] == ')')) {
                    if (IsLineMarker(line, digitEnd, 1)) {
                        return line.Insert(digitEnd, "\\");
                    }
                }
            }

            return line;
        }

        private static bool IsSetextUnderline(string line, int index)
        {
            var trimmed = line[index..].TrimEnd();
            if (trimmed.Length < 3) {
                return false;
            }

            var first = trimmed[0];
            if (first != '-' && first != '=') {
                return false;
            }

            for (var i = 1; i < trimmed.Length; i++) {
                if (trimmed[i] != first) {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLineMarker(string line, int markerIndex, int minTrailingSpaces)
        {
            var nextIndex = markerIndex + 1;
            if (nextIndex >= line.Length) {
                return false;
            }

            var spaceCount = 0;
            while (nextIndex < line.Length && line[nextIndex] == ' ') {
                spaceCount++;
                nextIndex++;
            }

            return spaceCount >= minTrailingSpaces;
        }
    }
}
