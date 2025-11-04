using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Strong : ConverterBase
    {
        public Strong(Converter converter) : base(converter)
        {
            Converter.Register("strong", this);
            Converter.Register("b", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node);
            if (string.IsNullOrEmpty(content) || AlreadyBold())
            {
                return content;
            }
            
            var spaceSuffix = node.NextSibling?.Name is "strong" or "b"
                ? " "
                : "";

            var emphasis = Converter.Config.SlackFlavored ? "*" : "**";
            return content.EmphasizeContentWhitespaceGuard(emphasis, spaceSuffix);
        }

        private bool AlreadyBold()
        {
            return Context.AncestorsAny("strong") || Context.AncestorsAny("b");
        }
    }
}
