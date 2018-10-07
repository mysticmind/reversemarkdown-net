using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Img : ConverterBase
    {
        public Img(Converter converter) : base(converter)
        {
            Converter.Register("img", this);
        }

        public override string Convert(HtmlNode node)
        {
            var alt = node.GetAttributeValue("alt", string.Empty);
            var src = node.GetAttributeValue("src", string.Empty);
            var title = ExtractTitle(node);

            title = title.Length > 0 ? $" \"{title}\"" : "";

            return $"![{alt}]({src}{title})";
        }
    }
}
