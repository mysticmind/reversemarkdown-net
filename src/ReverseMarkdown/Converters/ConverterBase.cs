
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public abstract class ConverterBase : IConverter
    {
        protected ConverterBase(Converter converter) 
        {
            Converter = converter;
        }

        protected Converter Converter { get; }

        protected string TreatChildren(HtmlNode node)
        {
            var result = string.Empty;

            return !node.HasChildNodes 
                ? result 
                : node.ChildNodes.Aggregate(result, (current, nd) => current + Treat(nd));
        }

        private string Treat(HtmlNode node) {
            return Converter.Lookup(node.Name).Convert(node);
        }

        protected string ExtractTitle(HtmlNode node)
        {
            var title = node.GetAttributeValue("title", "");

            return title;
        }

        protected string DecodeHtml(string html)
        {
            return System.Net.WebUtility.HtmlDecode(html);
        }

        public abstract string Convert(HtmlNode node);
    }
}
