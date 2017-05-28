using HtmlAgilityPack;
using static System.Environment;

namespace ReverseMarkdown.Converters
{
    public class Ol
        : ConverterBase
    {
        public Ol(Converter converter)
            : base(converter)
        {
            Converter.Register("ol", this);
            Converter.Register("ul", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"{NewLine}{TreatChildren(node)}{NewLine}";
        }
    }
}