using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Ignore: ConverterBase
	{
		public Ignore(Converter converter)
			: base(converter)
		{
			this.Converter.Register("colgroup", this);
			this.Converter.Register("col", this);
		}

		public override string Convert(HtmlNode node)
		{
			return "";
		}
	}
}
