using System;
using System.Collections.Generic;
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
            string content;

            do
            {
                if (node.ChildNodes.Count == 1 && node.FirstChild.Name == "div")
                {
                    node = node.FirstChild;
                    continue;
                }

                content = TreatChildren(node);
                break;
            } while (true);

            var blockTags = new List<string>
            {
                "pre",
                "p",
                "ol",
                "oi",
                "table"
            };

            // if there is a block child then ignore adding the newlines for div
            if ((node.ChildNodes.Count == 1 && blockTags.Contains(node.FirstChild.Name)))
            {
                return content;
            }

            return $"{(Td.FirstNodeWithinCell(node) ? "" : Environment.NewLine)}{content}{(Td.LastNodeWithinCell(node) ? "" : Environment.NewLine)}";
        }
    }
}
