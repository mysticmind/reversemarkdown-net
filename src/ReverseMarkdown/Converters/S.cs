using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class S : ConverterBase
    {
        public S(Converter converter) : base(converter)
        {
            Converter.Register("s", this);
            Converter.Register("del", this);
            Converter.Register("strike", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node);
            if (string.IsNullOrEmpty(content) || AlreadyStrikethrough())
            {
                return content;
            }

            var emphasis = Converter.Config.SlackFlavored ? "~" : "~~";
            return content.EmphasizeContentWhitespaceGuard(emphasis);
        }

        private bool AlreadyStrikethrough()
        {
            return Context.AncestorsAny("s") || Context.AncestorsAny("del") || Context.AncestorsAny("strike");
        }
    }
}
