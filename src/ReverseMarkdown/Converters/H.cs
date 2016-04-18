
using System;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class H
		: ConverterBase
	{
		public H(Converter converter)
			: base(converter)
		{
			this.Converter.Register("h1", this);
			this.Converter.Register("h2", this);
			this.Converter.Register("h3", this);
			this.Converter.Register("h4", this);
			this.Converter.Register("h5", this);
			this.Converter.Register("h6", this);
		}

		public override string Convert(HtmlNode node)
		{
			string prefix = new string('#', System.Convert.ToInt32(node.Name.Substring(1)));

			return Environment.NewLine + prefix + " " + this.TreatChildren(node) + Environment.NewLine;
		}
	}
}
