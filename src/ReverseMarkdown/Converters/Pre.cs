using System;
using System.IO;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public partial class Pre : ConverterBase {
        public Pre(Converter converter) : base(converter)
        {
            Converter.Register("pre", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Converter.Config.ConvertPreContentAsHtml) {
                ConvertHtmlContent(writer, node);
                return;
            }

            var isTelegram = Converter.Config.TelegramMarkdownV2;
            var isFencedCodeBlock = Converter.Config.GithubFlavored || Converter.Config.CommonMark || isTelegram;

            var indentation = (Converter.Config.CommonMark || isTelegram)
                ? string.Empty
                : IndentationFor(node);
            var contentIndentation = indentation;

            // 4 space indent for code if it is not fenced code block
            if (!isFencedCodeBlock) {
                indentation += "    ";
                contentIndentation = indentation;
            }

            writer.WriteLine();
            writer.WriteLine();

            // content:
            var content = DecodeHtml(node.InnerText);
            if (isTelegram) {
                content = StringUtils.EscapeTelegramMarkdownV2Code(content);
            }

            if (isFencedCodeBlock) {
                var fence = Converter.Config.CommonMark
                    ? CreateCommonMarkFence(content)
                    : "```";
                var lang = GetLanguage(node);
                writer.Write(indentation);
                writer.Write(fence);
                writer.Write(lang);
                writer.WriteLine();
            }
            foreach (var line in content.ReadLines()) {
                writer.Write(contentIndentation);
                writer.WriteLine(line);
            }

            if (string.IsNullOrEmpty(content)) {
                if (!isFencedCodeBlock) writer.Write(contentIndentation);
                writer.WriteLine();
            }

            if (isFencedCodeBlock) {
                var fence = Converter.Config.CommonMark
                    ? CreateCommonMarkFence(content)
                    : "```";
                writer.Write(indentation);
                writer.Write(fence);
            }

            writer.WriteLine();
        }

        private void ConvertHtmlContent(TextWriter writer, HtmlNode node)
        {
            var contentNode = node.ChildNodes["code"] ?? node;
            if (contentNode.HasChildNodes) {
                foreach (var child in contentNode.ChildNodes) {
                    Converter.ConvertNode(writer, child);
                }

                return;
            }

            TreatChildren(writer, node);
        }


        private string? GetLanguage(HtmlNode node)
        {
            var language = GetLanguageFromHighlightClassAttribute(node);

            if (!Converter.Config.CommonMark && !string.IsNullOrEmpty(language)) {
                language = language.TrimEnd(';');
            }

            return !string.IsNullOrEmpty(language)
                ? language
                : Converter.Config.DefaultCodeBlockLanguage;
        }


        private static string GetLanguageFromHighlightClassAttribute(HtmlNode node)
        {
            var res = ClassMatch(node);

            // check parent node:
            // GitHub: <div class="highlight highlight-source-json"><pre>
            // BitBucket: <div class="codehilite language-json"><pre>
            if (!res.Success && node.ParentNode != null!) {
                res = ClassMatch(node.ParentNode);
            }

            // check child <code> node:
            // HighlightJs: <pre><code class="hljs language-json">
            if (!res.Success) {
                var cnode = node.ChildNodes["code"];
                if (cnode != null!) {
                    res = ClassMatch(cnode);
                }
            }

            return res.Success && res.Groups.Count == 3 ? res.Groups[2].Value : string.Empty;
        }

        /// <summary>
        /// Extracts class attribute syntax using: highlight-json, highlight-source-json, language-json, brush: language
        /// Returns the Language in Match.Groups[2]
        /// </summary>
        [GeneratedRegex(@"(highlight-source-|language-|highlight-|brush:\s)([^\s]+)")]
        private static partial Regex ClassRegex();

        /// <summary>
        /// Checks class attribute for language class identifiers for various
        /// common highlighters
        /// </summary>
        /// <param name="node">Node with class attribute</param>
        /// <returns>Match.Success and Match.Group[2] set to the language</returns>
        private static Match ClassMatch(HtmlNode node)
        {
            var val = node.GetAttributeValue("class", string.Empty);
            if (!string.IsNullOrEmpty(val)) {
                val = System.Net.WebUtility.HtmlDecode(val);
                return ClassRegex().Match(val);
            }

            return Match.Empty;
        }

        private static string CreateCommonMarkFence(string content)
        {
            var maxRun = 0;
            var currentRun = 0;
            foreach (var c in content) {
                if (c == '`') {
                    currentRun++;
                    if (currentRun > maxRun) {
                        maxRun = currentRun;
                    }
                }
                else {
                    currentRun = 0;
                }
            }

            var fenceLength = Math.Max(3, maxRun + 1);
            return new string('`', fenceLength);
        }
    }
}
