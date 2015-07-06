using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Ol: ConverterBase
	{
		public Ol(Converter converter)
			: base(converter)
		{
			this.Converter.Register("ol", this);
			this.Converter.Register("ul", this);
		}

		public override string Convert(HtmlNode node)
		{
			return Environment.NewLine + this.TreatChildren(node);
		}
	}
}
