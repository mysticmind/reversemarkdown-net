using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public abstract class ConverterBase(Converter converter) : IConverter {
        protected Converter Converter { get; } = converter;
        protected ConverterContext Context => Converter.Context;

        protected void TreatChildren(TextWriter writer, HtmlNode node)
        {
            if (node.HasChildNodes) {
                foreach (var child in node.ChildNodes) {
                    Converter.ConvertNode(writer, child);
                }
            }
        }

        protected string TreatChildrenAsString(HtmlNode node)
        {
            if (node.HasChildNodes) {
                using var writer = Converter.CreateWriter(node);
                foreach (var child in node.ChildNodes) {
                    Converter.ConvertNode(writer, child);
                }

                return writer.ToString();
            }

            return string.Empty;
        }

        protected static string ExtractTitle(HtmlNode node)
        {
            return node.GetAttributeValue("title", string.Empty);
        }

        protected static string DecodeHtml(string html)
        {
            return System.Net.WebUtility.HtmlDecode(html);
        }

        protected static void DecodeHtml(TextWriter writer, string html)
        {
            System.Net.WebUtility.HtmlDecode(html, writer);
        }

        protected string IndentationFor(HtmlNode node, bool zeroIndex = false)
        {
            var length = Context.AncestorsCount("ol") + Context.AncestorsCount("ul");

            // li not required to have a parent ol/ul
            if (length == 0) {
                return string.Empty;
            }

            if (zeroIndex) {
                length -= 1;
            }

            return new string(' ', length * 4);
        }

        public abstract void Convert(TextWriter writer, HtmlNode node);
    }
}
