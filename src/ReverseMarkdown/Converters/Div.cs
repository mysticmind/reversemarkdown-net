using System;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Div : ConverterBase
    {
        public Div(Converter converter) : base(converter)
        {
            Converter.Register("div", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node).Trim();

            // if child is a pre tag then Trim in the above step removes the 4 spaces for code block
            if (node.ChildNodes.Count > 0 && node.FirstChild.Name == "pre" && !Converter.Config.GithubFlavored)
            {
                content = $"    {content}";
            }

            return $"{(Td.FirstNodeWithinCell(node) ? "" : Environment.NewLine)}{content}{(Td.LastNodeWithinCell(node) ? "" : Environment.NewLine)}";
        }
    }
}
