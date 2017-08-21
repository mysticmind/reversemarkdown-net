
using System;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Aside
		: ConverterBase
	{
		public Aside(Converter converter)
			: base(converter)
		{
			this.Converter.Register("aside", this);
		}

		public override string Convert(HtmlNode node)
		{
			return Environment.NewLine + this.TreatChildren(node).Trim() + Environment.NewLine;
		}
	}
}
