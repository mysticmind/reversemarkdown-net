using System.Linq;
using HtmlAgilityPack;
using static System.Net.WebUtility;
using static System.String;

namespace ReverseMarkdown.Converters
{
    public abstract class ConverterBase
        : IConverter
    {
        protected ConverterBase(Converter converter)
        {
            Converter = converter;
        }

        protected Converter Converter { get; }

        public abstract string Convert(HtmlNode node);

        public string TreatChildren(HtmlNode node)
        {

            // TreatChildren is one of the most frequently called routines so it needs to maximally optimized.

            return !node.HasChildNodes
                ? Empty
                : node
                    .ChildNodes
                    .Aggregate(Empty, (current, nd) => current + Treat(nd));
        }

        public string Treat(HtmlNode node)
        {
            return Converter.Lookup(node.Name).Convert(node);
        }

        public string ExtractTitle(HtmlNode node)
        {
            return node.GetAttributeValue("title", Empty);
        }

        public string DecodeHtml(string html)
        {
            return HtmlDecode(html);
        }
    }
}