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

            content = Converter.Config.CleanupUnnecessarySpaces ? content.Trim() : content;

            // if there is a block child then ignore adding the newlines for div
            if (
                node.ChildNodes.Count == 1 &&
                node.FirstChild.Name
                    is "pre"
                    or "p"
                    or "ol"
                    or "oi"
                    or "table"
            )
            {
                return content;
            }

            var prefix = Environment.NewLine;

            if (Td.FirstNodeWithinCell(node))
            {
                prefix = string.Empty;
            } 
            else if (Converter.Config.SuppressDivNewlines)
            {
                prefix = string.Empty;
            }
            
            return $"{prefix}{content}{(Td.LastNodeWithinCell(node) ? "" : Environment.NewLine)}";
        }
    }
}
