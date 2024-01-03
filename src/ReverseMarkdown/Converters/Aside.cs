using System;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Aside : ConverterBase
    {
        public Aside(Converter converter)
            : base(converter)
        {
            Converter.Register("aside", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"{Environment.NewLine}{TreatChildren(node)}{Environment.NewLine}";
        }
    }
}
