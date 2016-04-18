
using System;
using System.Linq;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class P
		: ConverterBase
	{
		public P(Converter converter)
			: base(converter)
		{
			this.Converter.Register("p", this);
		}

		public override string Convert(HtmlNode node)
		{
			string indentation = IndentationFor(node);
			return indentation + this.TreatChildren(node).Trim() + Environment.NewLine + Environment.NewLine;
		}

		private string IndentationFor(HtmlNode node)
		{
			int length = node.Ancestors("ol").Count() + node.Ancestors("ul").Count();
			return node.ParentNode.Name.ToLowerInvariant() == "li" && node.ParentNode.FirstChild != node
				? new string(' ', length * 4)
				: Environment.NewLine + Environment.NewLine;
		}
	}
}
