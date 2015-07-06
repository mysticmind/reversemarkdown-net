using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class P: ConverterBase
	{
		public P(Converter converter):base(converter)
		{
			this.Converter.Register("p", this);
		}

		public override string Convert(HtmlNode node)
		{
			return Environment.NewLine + Environment.NewLine + this.TreatChildren(node).Trim() + Environment.NewLine + Environment.NewLine;
		}
	}
}
