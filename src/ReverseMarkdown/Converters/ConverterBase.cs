
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
            TrimNewLine(node);
            return Converter.Lookup(node.Name).Convert(node);
        }

        private static void TrimNewLine(HtmlNode node)
        {
            if (!node.HasChildNodes) return;

            if (node.FirstChild.Name == "#text" && (node.FirstChild.InnerText.StartsWith("\r\n") || node.FirstChild.InnerText.StartsWith("\n")))
            {
                node.FirstChild.InnerHtml = node.FirstChild.InnerHtml.TrimStart('\r').TrimStart('\n');
            }

            if (node.LastChild.Name == "#text" && (node.LastChild.InnerText.EndsWith("\r\n") || node.LastChild.InnerText.EndsWith("\n")))
            {
                node.LastChild.InnerHtml = node.LastChild.InnerHtml.TrimEnd('\r').TrimEnd('\n');
            }
        }

        protected static string ExtractTitle(HtmlNode node)
        {
            return node.GetAttributeValue("title", "");
        }

        protected static string DecodeHtml(string html)
        {
            return System.Net.WebUtility.HtmlDecode(html);
        }

        protected static string IndentationFor(HtmlNode node, bool zeroIndex=false)
        {
            var length = node.Ancestors("ol").Count() + node.Ancestors("ul").Count();

            // li not required to have a parent ol/ul
            if (length == 0)
            {
                return string.Empty;
            }

            if (zeroIndex)
            {
                length -= 1;
            }

            return new string(' ', length*4);
        }

        public abstract string Convert(HtmlNode node);
    }
}
