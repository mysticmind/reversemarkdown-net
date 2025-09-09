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
            if (Converter.Config.SlackFlavored)
            {
                throw new SlackUnsupportedTagException(node.Name);
            }
            
            var alt = node.GetAttributeValue("alt", string.Empty);
            var src = node.GetAttributeValue("src", string.Empty);

            var schema = StringUtils.GetScheme(src);
            if (!Converter.Config.IsSchemeWhitelisted(schema))
            {
                return "";
            }

            var title = ExtractTitle(node);
            title = title.Length > 0 ? $" \"{title}\"" : "";

            return $"![{StringUtils.EscapeLinkText(alt)}]({src}{title})";
        }
    }
}
