using HtmlAgilityPack;
using System;

namespace ReverseMarkdown.Converters
{
    public class Td : ConverterBase
    {
        public Td(Converter converter) : base(converter)
        {
            var elements = new [] { "td", "th" };

            foreach (var element in elements)
            {
                Converter.Register(element, this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node)
                .Replace(Environment.NewLine, "<br>");

            return $" {content} |";
        }
    }
}
