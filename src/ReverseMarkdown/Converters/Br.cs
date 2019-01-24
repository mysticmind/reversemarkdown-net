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
            return Converter.Config.GithubFlavored ? Environment.NewLine : $"  {Environment.NewLine}";
        }
    }
}
