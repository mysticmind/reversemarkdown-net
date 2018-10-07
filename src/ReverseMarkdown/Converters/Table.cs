using System;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Table : ConverterBase
    {
        public Table(Converter converter) : base(converter)
        {
            Converter.Register("table", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"{Environment.NewLine}{Environment.NewLine}{TreatChildren(node)}{Environment.NewLine}";
        }
    }
}
