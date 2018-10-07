using System;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Div : ConverterBase
    {
        public Div(Converter converter) : base(converter)
        {
            Converter.Register("div", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"{Environment.NewLine}{TreatChildren(node).Trim()}{Environment.NewLine}";
        }
    }
}
