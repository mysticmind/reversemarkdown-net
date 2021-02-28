using System;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Pre : ConverterBase
    {
        public Pre(Converter converter) : base(converter)
        {
            Converter.Register("pre", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = DecodeHtml(node.InnerText);

            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            // check if indentation need to be added if it is under a ordered or unordered list
            var indentation = IndentationFor(node);

            var fencedCodeStartBlock = string.Empty;
            var fencedCodeEndBlock = string.Empty;

            if (Converter.Config.GithubFlavored)
            {
                var lang = GetLanguage(node);
                fencedCodeStartBlock = $"{indentation}```{lang}{Environment.NewLine}";
                fencedCodeEndBlock = $"{indentation}```{Environment.NewLine}";
            }
            else
            {
                // 4 space indent for code if it is not fenced code block
                indentation += "    ";
            }

            var lines = content.ReadLines().Select(item => indentation + item);
            content = string.Join(Environment.NewLine, lines);

            return $"{Environment.NewLine}{Environment.NewLine}{fencedCodeStartBlock}{content}{Environment.NewLine}{fencedCodeEndBlock}{Environment.NewLine}";
        }

        private string GetLanguage(HtmlNode node)
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
            if (!res.Success && node.ParentNode != null)
            {
                res = ClassMatch(node.ParentNode);
            }

            // check child <code> node:
            // HighlightJs: <pre><code class="hljs language-json">
            if (!res.Success)
            {
                var cnode = node.ChildNodes["code"];
                if (cnode != null)
                {
                    res = ClassMatch(cnode);
                }
            }
			
            return res.Success && res.Groups.Count == 3 ? res.Groups[2].Value : string.Empty;
        }

        /// <summary>
        /// Extracts class attribute syntax using: highlight-json, highlight-source-json, language-json, brush: language
        /// Returns the Language in Match.Groups[2]
        /// </summary>
        private static readonly Regex ClassRegex = new Regex(@"(highlight-source-|language-|highlight-|brush:\s)([a-zA-Z0-9]+)");

        /// <summary>
        /// Checks class attribute for language class identifiers for various
        /// common highlighters
        /// </summary>
        /// <param name="node">Node with class attribute</param>
        /// <returns>Match.Success and Match.Group[2] set to the language</returns>
        private static Match ClassMatch(HtmlNode node)
        {
            var val = node.GetAttributeValue("class", "");
            if (!string.IsNullOrEmpty(val))
            {
                return ClassRegex.Match(val);
            }

            return Match.Empty;
        }
    }
}
