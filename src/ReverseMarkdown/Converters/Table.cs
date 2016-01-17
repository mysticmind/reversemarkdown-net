
using System;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Table
		: ConverterBase
	{
		public Table(Converter converter)
			: base(converter)
		{
			this.Converter.Register("table", this);
		}

		public override string Convert(HtmlNode node)
		{
			return Environment.NewLine + Environment.NewLine + this.TreatChildren(node) + Environment.NewLine;
		}
	}
}
