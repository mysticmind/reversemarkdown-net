
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public abstract class ConverterBase
		: IConverter
	{
		private Converter _converter;

		public ConverterBase(Converter converter) 
		{
			this._converter = converter;
		}

		protected Converter Converter 
		{
			get 
			{
				return this._converter;
			}
		}

		public string TreatChildren(HtmlNode node)
		{
			string result = string.Empty;

			if (node.HasChildNodes)
			{
				foreach(HtmlNode nd in node.ChildNodes)
				{
					result += this.Treat(nd);
				}
			}

			return result;
		}

		public string Treat(HtmlNode node) {
			return this.Converter.Lookup(node.Name).Convert(node);
		}

		public string ExtractTitle(HtmlNode node)
		{
			string title = node.GetAttributeValue("title", "");

			return title;
		}

		public string DecodeHtml(string html)
		{
			return System.Net.WebUtility.HtmlDecode(html);
		}

		public abstract string Convert(HtmlNode node); 
	}
}
