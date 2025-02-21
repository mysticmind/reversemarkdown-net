using System;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Hr : ConverterBase
    {
        public Hr(Converter converter) : base(converter)
        {
            Converter.Register("hr", this);
        }

        public override string Convert(HtmlNode node)
        {
            if (Converter.Config.SlackFlavored)
            {
                throw new SlackUnsupportedTagException(node.Name);
            }
            
            return $"{Environment.NewLine}* * *{Environment.NewLine}";
        }
    }
}
