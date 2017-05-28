using HtmlAgilityPack;
using static System.Environment;

namespace ReverseMarkdown.Converters
{
    public class Div
        : ConverterBase
    {
        public Div(Converter converter)
            : base(converter)
        {
            Converter.Register("div", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"{NewLine}{TreatChildren(node).Trim()}{NewLine}";
        }
    }
}