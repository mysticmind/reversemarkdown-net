using System;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Br : ConverterBase
    {
        public Br(Converter converter) : base(converter)
        {
            Converter.Register("br", this);
        }

        public override string Convert(HtmlNode node)
        {
            var parentName = node.ParentNode.Name.ToLowerInvariant();
            if (parentName is "strong" or "b" or "em" or "i")
            {
                return "";
            }

            return Converter.Config.GithubFlavored ? Environment.NewLine : $"  {Environment.NewLine}";
        }
    }
}
