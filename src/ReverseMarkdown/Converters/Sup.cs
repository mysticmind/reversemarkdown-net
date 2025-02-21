using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Sup : ConverterBase
    {
        public Sup(Converter converter) : base(converter)
        {
            Converter.Register("sup", this);   
        }

        public override string Convert(HtmlNode node)
        {
            if (Converter.Config.SlackFlavored)
            {
                throw new SlackUnsupportedTagException(node.Name);
            }
            
            var content = TreatChildren(node);
            if (string.IsNullOrEmpty(content) || AlreadySup(node))
            {
                return content;
            }

            return $"^{content.Chomp(all:true)}^";
        }

        private static bool AlreadySup(HtmlNode node)
        {
            return node.Ancestors("sup").Any();
        }
    }
}
