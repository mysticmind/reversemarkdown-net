using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Div: ConverterBase
	{
		public Div(Converter converter):base(converter)
		{
			this.Converter.Register("div", this);
		}

		public override string Convert(HtmlNode node)
		{
			return Environment.NewLine + this.TreatChildren(node).Trim() + Environment.NewLine;
		}
	}
}
