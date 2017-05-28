using System.Linq;
using HtmlAgilityPack;
using static System.String;

namespace ReverseMarkdown.Converters
{
    public class Em
        : ConverterBase
    {
        public Em(Converter converter)
            : base(converter)
        {
            Converter.Register("em", this);
            Converter.Register("i", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node);
            if (IsNullOrEmpty(content.Trim()) || AlreadyItalic(node))
                return content;

            return $"*{content.Trim()}*";
        }

        private static bool AlreadyItalic(HtmlNode node)
        {
            return node
                .Ancestors("i")
                .Any() || node
                    .Ancestors("em")
                    .Any();
        }
    }
}