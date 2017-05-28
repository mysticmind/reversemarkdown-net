using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Strong
        : ConverterBase
    {
        public Strong(Converter converter)
            : base(converter)
        {
            Converter.Register("strong", this);
            Converter.Register("b", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node);
            if (string.IsNullOrEmpty(content.Trim()) || AlreadyBold(node))
                return content;

            return $"**{content.Trim()}**";
        }

        private static bool AlreadyBold(HtmlNode node)
        {
            return node
                .Ancestors("strong")
                .Any() || node.Ancestors("b").Any();
        }
    }
}