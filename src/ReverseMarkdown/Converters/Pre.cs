using System;
using System.Linq;

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
                var code = DecodeHtml(node.InnerText);
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
            var lang = GetLanguageFromHighlightClassAttribute(node);
            return lang !=string.Empty ? lang : GetLanguageFromConfluenceClassAttribute(node); 
        }

        private static string GetLanguageFromHighlightClassAttribute(HtmlNode node)
        {
            var val = node.GetAttributeValue("class", "");
            var rx = new System.Text.RegularExpressions.Regex("highlight-([a-zA-Z0-9]+)");
            var res = rx.Match(val);
            return res.Success ? res.Value.Split('-')[1].Replace(";", "").Trim() : "";
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
