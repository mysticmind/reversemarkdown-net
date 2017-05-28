using HtmlAgilityPack;
using static System.String;

namespace ReverseMarkdown.Converters
{
    public class Img
        : ConverterBase
    {
        public Img(Converter converter)
            : base(converter)
        {
            Converter.Register("img", this);
        }

        public override string Convert(HtmlNode node)
        {
            var alt = node.GetAttributeValue("alt", Empty);
            var src = node.GetAttributeValue("src", Empty);
            var title = ExtractTitle(node);

            title = title.Length > 0
                ? $" \"{title}\""
                : Empty;

            return $"![{alt}]({src}{title})";
        }
    }
}