using HtmlAgilityPack;
using static System.Environment;

namespace ReverseMarkdown.Converters
{
    public class Hr
        : ConverterBase
    {
        public Hr(Converter converter)
            : base(converter)
        {
            Converter.Register("hr", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"{NewLine}* * *{NewLine}";
        }
    }
}