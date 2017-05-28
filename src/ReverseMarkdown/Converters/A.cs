using HtmlAgilityPack;
using System;

namespace ReverseMarkdown.Converters
{
    public class A
        : ConverterBase
    {
        public A(Converter converter)
            : base(converter)
        {
            Converter.Register("a", this);
        }

        public override string Convert(HtmlNode node)
        {
            var name = TreatChildren(node);

            var href = node.GetAttributeValue("href", string.Empty);
            var title = ExtractTitle(node);
            title = title.Length > 0
                ? string.Format(" \"{0}\"", title)
                : string.Empty;

            if (href.StartsWith("#", StringComparison.Ordinal) || string.IsNullOrEmpty(href) || string.IsNullOrEmpty(name))
                return name;

            return $"[{name}]({href}{title})";
        }
    }
}