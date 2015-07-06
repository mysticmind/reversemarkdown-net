using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Text : ConverterBase
	{
		public Text(Converter converter)
			: base(converter)
		{
			this.Converter.Register("#text", this);
		}

		public override string Convert(HtmlNode node)
		{
			string content = node.InnerText;
			content =  this.EscapeKeyChars(content);
			
			content = this.PreserveKeyCharswithinBackTicks(content);

			return content;
		}

		private string PreserveKeyCharswithinBackTicks(string content)
		{
			Regex rx = new Regex("`.*?`");

			content = rx.Replace(content, (Match p) =>
			{
				return p.Value.Replace(@"\*", "*").Replace(@"\_", "_");
			});

			return content;
		}
	}
}
