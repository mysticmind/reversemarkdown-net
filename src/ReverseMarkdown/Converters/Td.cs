using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Td
        : ConverterBase
    {
        public Td(Converter converter)
            : base(converter)
        {
            Converter.Register("td", this);
            Converter.Register("th", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $" {TreatChildren(node)} |";
        }
    }
}