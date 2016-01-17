
using System;
using System.Linq;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Blockquote
		: ConverterBase
	{
		public Blockquote(Converter converter)
			: base(converter)
		{
			this.Converter.Register("blockquote", this);
		}

		public override string Convert(HtmlNode node)
		{
			string content = this.TreatChildren(node).Trim();

			// get the lines based on carriage return and prefix "> " to each line
			var lines = content.ReadLines().Select(item => "> " + item + Environment.NewLine);

			// join all the lines to a single line
			var result = lines.Aggregate((curr, next) => curr + next);

			return Environment.NewLine + Environment.NewLine + result + Environment.NewLine;
		}
	}
}
