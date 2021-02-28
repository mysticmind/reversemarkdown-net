using HtmlAgilityPack;
using System.Net;
using System.Text;

namespace ReverseMarkdown.Converters
{
    public class Code : ConverterBase
    {
        public Code(Converter converter) : base(converter)
        {
            Converter.Register("code", this);
        }

        public override string Convert(HtmlNode node)
        {
            var sb = new StringBuilder();

            if (node.InnerText != node.InnerText.TrimStart())
            {
                sb.Append(' ');
            }

            sb.Append('`');
            sb.Append(WebUtility.HtmlDecode(node.InnerText.Trim()));
            sb.Append('`');

            if (node.InnerText != node.InnerText.TrimEnd())
            {
                sb.Append(' ');
            }

            return sb.ToString();
        }
    }
}
