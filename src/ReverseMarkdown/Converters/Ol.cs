using System;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
	public class Ol : ConverterBase
	{
		public Ol(Converter converter) : base(converter)
		{
			var elements = new [] { "ol", "ul" };

			foreach (var element in elements)
			{
				Converter.Register(element, this);
			}
		}

		public override string Convert(HtmlNode node)
		{
			return $"{Environment.NewLine}{TreatChildren(node)}{Environment.NewLine}";
		}
	}
}
