using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Code : ConverterBase
    {
        public Code(Converter converter) : base(converter)
        {
            Converter.Register("code", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"`{node.InnerText.Trim()}`";
        }
    }
}
