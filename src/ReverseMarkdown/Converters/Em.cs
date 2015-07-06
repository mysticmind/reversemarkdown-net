using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Em: ConverterBase
	{
		public Em(Converter converter)
			: base(converter)
		{
			this.Converter.Register("em", this);
			this.Converter.Register("i", this);
		}

		public override string Convert(HtmlNode node)
		{
			string content = this.TreatChildren(node);
			if (string.IsNullOrEmpty(content.Trim()) || AlreadyItalic(node))
			{
				return content;
			}
			else
			{
				return "*" + content.Trim() + "*";
			}
		}

		private bool AlreadyItalic(HtmlNode node)
		{
			return node.Ancestors("i").Count() > 0 || node.Ancestors("em").Count() > 0;
		}
	}
}
