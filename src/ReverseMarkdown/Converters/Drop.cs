using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class Drop : ConverterBase {
        public Drop(Converter converter) : base(converter)
        {
            Converter.Register("style", this);
            Converter.Register("script", this);
            if (Converter.Config.RemoveComments) {
                converter.Register("#comment", this);
            }
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            // Do nothing, effectively dropping the node and its children
        }
    }
}
