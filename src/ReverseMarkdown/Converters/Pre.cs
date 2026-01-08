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
            var isFencedCodeBlock = Converter.Config.GithubFlavored;

            // check if indentation need to be added if it is under an ordered or unordered list
            var indentation = IndentationFor(node);

            // 4 space indent for code if it is not fenced code block
            if (!isFencedCodeBlock) {
                indentation += "    ";
            }

            writer.WriteLine();
            writer.WriteLine();

            if (isFencedCodeBlock) {
                var lang = GetLanguage(node);
                writer.Write(indentation);
                writer.Write("```");
                writer.Write(lang);
                writer.WriteLine();
            }

            // content:
            var content = DecodeHtml(node.InnerText);
            foreach (var line in content.ReadLines()) {
                writer.Write(indentation);
                writer.WriteLine(line);
            }

            if (string.IsNullOrEmpty(content)) {
                if (!isFencedCodeBlock) writer.Write(indentation);
                writer.WriteLine();
            }

            if (isFencedCodeBlock) {
                writer.Write(indentation);
                writer.Write("```");
            }

            writer.WriteLine();
        }


        private string? GetLanguage(HtmlNode node)
        {
            var language = GetLanguageFromHighlightClassAttribute(node);

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
        [GeneratedRegex(@"(highlight-source-|language-|highlight-|brush:\s)([a-zA-Z0-9]+)")]
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
                return ClassRegex().Match(val);
            }

            return Match.Empty;
        }
    }
}
