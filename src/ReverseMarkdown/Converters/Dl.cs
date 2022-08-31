using System;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Dl : ConverterBase
    {
        public Dl(Converter converter) : base(converter)
        {
            Converter.Register("dl", this);
        }

        public override string Convert(HtmlNode node)
        {
            var prefixSuffix = Environment.NewLine;
            return $"{prefixSuffix}{TreatChildren(node)}{prefixSuffix}";
        }
    }    
}