using System.IO;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


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


        public static void TreatEmphasizeContentWhitespaceGuard(TextWriter writer, string content, string emphasis, string nextSiblingSpaceSuffix = "")
        {
            WriteLeadingSpace(writer, content);
            writer.Write(emphasis);
            writer.Write(content.Chomp());
            writer.Write(emphasis);
            WriteTrailingSpace(writer, content, nextSiblingSpaceSuffix);

            return;

            static void WriteLeadingSpace(TextWriter writer, string content)
            {
                foreach (var c in content) {
                    if (c != ' ') break;
                    writer.Write(c);
                }
            }

            static void WriteTrailingSpace(TextWriter writer, string content, string nextSiblingSpaceSuffix)
            {
                var start = content.Length - 1;
                var i = start;
                for (; i >= 0; i--) {
                    var c = content[i];
                    if (c != ' ') break;
                    writer.Write(c);
                }

                if (i == start) {
                    writer.Write(nextSiblingSpaceSuffix);
                }
            }
        }


        public abstract void Convert(TextWriter writer, HtmlNode node);
    }
}
