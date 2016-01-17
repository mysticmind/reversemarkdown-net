
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Td
		: ConverterBase
	{
		public Td(Converter converter)
			: base(converter)
		{
			this.Converter.Register("td", this);
			this.Converter.Register("th", this);
		}

		public override string Convert(HtmlNode node)
		{
			string content = this.TreatChildren(node);
			return string.Format(" {0} |", content);
		}
	}
}
