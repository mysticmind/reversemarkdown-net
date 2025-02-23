﻿using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Strong : ConverterBase
    {
        public Strong(Converter converter) : base(converter)
        {
            var elements = new [] { "strong", "b" };

            foreach (var element in elements)
            {
                Converter.Register(element, this);
            }
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node);
            if (string.IsNullOrEmpty(content) || AlreadyBold(node))
            {
                return content;
            }
            
            var spaceSuffix = (node.NextSibling?.Name == "strong" || node.NextSibling?.Name == "b")
                ? " "
                : "";

            var emphasis = Converter.Config.SlackFlavored ? "*" : "**";
            return content.EmphasizeContentWhitespaceGuard(emphasis, spaceSuffix);
        }

        private static bool AlreadyBold(HtmlNode node)
        {
            return node.Ancestors("strong").Any() || node.Ancestors("b").Any();
        }
    }
}
