using HtmlAgilityPack;
using System;
using System.Linq;
using System.Text;

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
            // Standardize whitespace before inner lists so that the following are equivalent
            //   <li>Foo<ul><li>...
            //   <li>Foo\n    <ul><li>...
            foreach (var innerList in node.SelectNodes("//ul|//ol") ?? Enumerable.Empty<HtmlNode>())
            {
                if (innerList.PreviousSibling?.NodeType == HtmlNodeType.Text)
                {
                    innerList.PreviousSibling.InnerHtml = innerList.PreviousSibling.InnerHtml.Chomp();
                }
            }

            var content = ContentFor(node);
            var indentation = IndentationFor(node, true);
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
                return $"{Converter.Config.ListBulletChar} ";
            }
        }

        private string ContentFor(HtmlNode node)
        {
            if (!Converter.Config.GithubFlavored)
                return TreatChildren(node);

            var content = new StringBuilder();

            if (node.FirstChild is HtmlNode childNode
                && childNode.Name == "input"
                && childNode.GetAttributeValue("type", "").Equals("checkbox", StringComparison.OrdinalIgnoreCase))
            {
                content.Append(childNode.Attributes.Contains("checked")
                    ? $"[x]"
                    : $"[ ]");

                node.RemoveChild(childNode);
            }

            content.Append(TreatChildren(node));
            return content.ToString();
        }
    }
}
