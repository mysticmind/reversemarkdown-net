using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using static System.Environment;
using static System.String;

namespace ReverseMarkdown.Converters
{
    public class Pre
        : ConverterBase
    {
        public Pre(Converter converter)
            : base(converter)
        {
            Converter.Register("pre", this);
        }

        public override string Convert(HtmlNode node)
        {
            if (Converter.Config.GithubFlavored)
            {
                return
                   $"{NewLine}```{GetLanguage(node)}{NewLine}{DecodeHtml(node.InnerText)}{NewLine}```{NewLine}";
            }
            // get the lines based on carriage return and prefix four spaces to each line

            var lines = node
                .InnerText
                .ReadLines()
                .Select(item => $"    {item}{NewLine}");

            // join all the lines to a single line
            var result = lines.Aggregate(Empty, (curr, next) => curr + next);

            return $"{NewLine}{NewLine}{result}{NewLine}";
        }

        private static string GetLanguage(HtmlNode node)
        {
            var lang = GetLanguageFromHighlightClassAttribute(node);
            return lang != Empty
                ? lang
                : GetLanguageFromConfluenceClassAttribute(node);
        }

        private static string GetLanguageFromHighlightClassAttribute(HtmlNode node)
        {
            var val = node.GetAttributeValue("class", Empty);
            var regex = new Regex("highlight-([a-zA-Z0-9]+)", RegexOptions.Compiled);
            return regex.Match(val).Success
                ? regex.Match(val).Value
                : Empty;
        }

        private static string GetLanguageFromConfluenceClassAttribute(HtmlNode node)
        {
            var val = node.GetAttributeValue("class", Empty);
            var regex = new Regex(@"brush:\s?(:?.*);", RegexOptions.Compiled);
            return regex.Match(val).Success
                ? regex.Match(val).Value
                : Empty;
        }
    }
}