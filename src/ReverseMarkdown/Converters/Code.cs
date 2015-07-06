using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Code: ConverterBase
	{
		public Code(Converter converter):base(converter)
		{
			this.Converter.Register("code", this);
		}

		public override string Convert(HtmlNode node)
		{
			return "`" + node.InnerText.Trim() + "`";
		}
	}
}
