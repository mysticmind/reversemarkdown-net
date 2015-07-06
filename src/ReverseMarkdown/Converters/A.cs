
using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class A : ConverterBase
	{
		public A(Converter converter):base(converter)
		{
			this.Converter.Register("a", this);
		}

		public override string Convert(HtmlNode node)
		{
			string name = this.TreatChildren(node);

			string href = node.GetAttributeValue("href", string.Empty);
			string title = this.ExtractTitle(node);
			title = title.Length > 0 ? string.Format(" \"{0}\"", title) : "";

			if (href.StartsWith("#") || string.IsNullOrEmpty(href) || string.IsNullOrEmpty(name))
			{
				return name;
			}
			else
			{
				return string.Format("[{0}]({1}{2})", name, href, title);
			}
		}
	}
}
