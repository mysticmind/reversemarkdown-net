using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Ignore : ConverterBase
    {
        public Ignore(Converter converter) : base(converter)
        {
            var elements = new [] { "colgroup", "col" };

            foreach (var element in elements)
            {
                Converter.Register(element, this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            return "";
        }
    }
}
