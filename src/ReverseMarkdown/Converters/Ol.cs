using System;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Ol : ConverterBase
    {
        public Ol(Converter converter) : base(converter)
        {
            Converter.Register("ol", this);
            Converter.Register("ul", this);
        }

        public override string Convert(HtmlNode node)
        {
            // Lists inside tables are not supported as markdown, so leave as HTML
            if (Context.AncestorsAny("table"))
            {
                return node.OuterHtml;
            }

            string prefixSuffix = Environment.NewLine;

            // Prevent blank lines being inserted in nested lists
            string parentName = node.ParentNode.Name;
            if (parentName is "ol" or "ul")
            {
                prefixSuffix = "";
            }

            return $"{prefixSuffix}{TreatChildren(node)}{prefixSuffix}";
        }
    }
}
