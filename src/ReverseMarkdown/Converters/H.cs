using System;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class H : ConverterBase
    {
        public H(Converter converter) : base(converter)
        {
            var elements = new [] { "h1", "h2", "h3", "h4", "h5", "h6" };
            foreach (var element in elements)
            {
                Converter.Register(element, this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            // Headings inside tables are not supported as markdown, so just ignore the heading and convert children
            if (node.Ancestors("table").Any())
            {
                return TreatChildren(node);
            }

            var prefix = new string('#', System.Convert.ToInt32(node.Name.Substring(1)));

            return $"{Environment.NewLine}{prefix} {TreatChildren(node)}{Environment.NewLine}";
        }
    }
}
