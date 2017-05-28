using HtmlAgilityPack;
using static System.String;

namespace ReverseMarkdown.Converters
{
    public class Ignore
        : ConverterBase
    {
        public Ignore(Converter converter)
            : base(converter)
        {
            Converter.Register("colgroup", this);
            Converter.Register("col", this);
        }

        public override string Convert(HtmlNode node)
        {
            return Empty;
        }
    }
}