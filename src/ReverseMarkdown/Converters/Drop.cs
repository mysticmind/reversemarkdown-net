using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Drop : ConverterBase
    {
        public Drop(Converter converter) : base(converter)
        {
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
