using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class ByPass : ConverterBase
    {
        public ByPass(Converter converter) : base(converter)
        {
        }

        public override string Convert(HtmlNode node)
        {
            return TreatChildren(node);
        }
    }
}
