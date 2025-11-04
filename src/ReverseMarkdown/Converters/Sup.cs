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
            if (string.IsNullOrEmpty(content) || AlreadySup())
            {
                return content;
            }

            return $"^{content.Chomp(all:true)}^";
        }

        private bool AlreadySup()
        {
            return Context.AncestorsAny("sup");
        }
    }
}
