using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Drop : ConverterBase
    {
        public Drop(Converter converter) : base(converter)
        {
            Converter.Register("style", this);
            Converter.Register("script", this);
            if (Converter.Config.RemoveComments) {
                converter.Register("#comment", this);
            }

            if (Converter.Config.SkipHeaderFooter)
            {
                converter.Register("header", this);
                converter.Register("footer", this);
            }

            if (Converter.Config.SkipNav)
            {
                converter.Register("nav", this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            return "";
        }
    }
}
