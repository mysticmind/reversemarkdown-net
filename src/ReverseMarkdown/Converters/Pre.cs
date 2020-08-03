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
            if (Converter.Config.GithubFlavored)
            {
                var lang = GetLanguage(node);
                var code = DecodeHtml(node.InnerText).TrimEnd(new [] {'\n','\r'});
                return $"{Environment.NewLine}```{lang}{Environment.NewLine}{code}{Environment.NewLine}```{Environment.NewLine}";
            }
            else
            {
                // get the lines based on carriage return and prefix four spaces to each line
                var lines = node.InnerText.ReadLines().Select(item => $"    {item}{Environment.NewLine}");

                // join all the lines to a single line
                var result = lines.Aggregate(string.Empty, (curr, next) => curr + next);

                return $"{Environment.NewLine}{Environment.NewLine}{result}{Environment.NewLine}";
            }
        }

        private string GetLanguage(HtmlNode node)
        {
            return GetLanguageFromHighlightClassAttribute(node);
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

        private static string GetLanguageFromConfluenceClassAttribute(HtmlNode node)
        {
            var val = node.GetAttributeValue("class", "");
            var rx = new System.Text.RegularExpressions.Regex(@"brush:\s?(:?.*)");
            var res = rx.Match(val);
            return res.Success ? res.Value.Split(':')[1].Replace(";","").Trim() : "";
        }
    }
}
