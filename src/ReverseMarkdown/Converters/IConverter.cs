using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public interface IConverter
	{
		string Convert(HtmlNode node);
	}
}
