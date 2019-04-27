using System;
using System.Linq;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Li : ConverterBase
    {
        public Li(Converter converter) : base(converter)
        {
            Converter.Register("li", this);
        }

        public override string Convert(HtmlNode node)
        {
            var content = TreatChildren(node);
            var indentation = IndentationFor(node);
            var prefix = PrefixFor(node);

            return $"{indentation}{prefix}{content.Chomp()}{Environment.NewLine}";
        }

        private string PrefixFor(HtmlNode node)
        {
            if (node.ParentNode != null && node.ParentNode.Name == "ol")
            {
                // index are zero based hence add one
                var index = node.ParentNode.SelectNodes("./li").IndexOf(node) + 1;
                return $"{index}. ";
            }
            else
            {
                return "- ";
            }
        }
        
        private static string IndentationFor(HtmlNode node)
        {
            var length = node.Ancestors("ol").Count() + node.Ancestors("ul").Count();
            return new string(' ', Math.Max(length-1,0)*4);
        }
    }
}
