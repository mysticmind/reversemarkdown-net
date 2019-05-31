using System;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Ol : ConverterBase
    {
        public Ol(Converter converter) : base(converter)
        {
            var elements = new[] { "ol", "ul" };

            foreach (var element in elements)
            {
                Converter.Register(element, this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            // Lists inside tables are not supported as markdown, so leave as HTML
            if (node.Ancestors("table").Any())
            {
                return node.OuterHtml;
            }

            string prefixSuffix = Environment.NewLine;

            // Prevent blank lines being inserted in nested lists
            string parentName = node.ParentNode.Name.ToLowerInvariant();
            if (parentName == "ol" || parentName == "ul")
            {
                prefixSuffix = "";
            }

            return $"{prefixSuffix}{TreatChildren(node)}{prefixSuffix}";
        }
    }
}
