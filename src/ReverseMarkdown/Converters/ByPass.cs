
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class ByPass
		: ConverterBase
	{
		public ByPass(Converter converter)
			: base(converter)
		{
			this.Converter.Register("#document", this);
			this.Converter.Register("html", this);
			this.Converter.Register("body", this);
			this.Converter.Register("span", this);
			this.Converter.Register("thead", this);
			this.Converter.Register("tbody", this);
		}

		public override string Convert(HtmlNode node)
		{
			return this.TreatChildren(node);
		}
	}
}
