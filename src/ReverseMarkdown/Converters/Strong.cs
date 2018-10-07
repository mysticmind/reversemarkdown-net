using System.Linq;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Strong : ConverterBase
    {
        public Strong(Converter converter) : base(converter)
        {
            var elements = new [] { "strong", "b" };
            
            foreach (var element in elements)
            {
                Converter.Register(element, this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            var content = this.TreatChildren(node);
            if (string.IsNullOrEmpty(content.Trim()) || AlreadyBold(node))
            {
                return content;
            }
            else
            {
                return $"**{content.Trim()}**";
            }
        }

        private static bool AlreadyBold(HtmlNode node)
        {
            return node.Ancestors("strong").Any() || node.Ancestors("b").Any();
        }
    }
}
