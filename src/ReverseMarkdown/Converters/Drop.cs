using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Drop : ConverterBase
    {
        public Drop(Converter converter) : base(converter)
        {
            Converter.Register("style", this);
            if (Converter.Config.RemoveComments) {
                converter.Register("#comment", this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            return "";
        }
    }
}
