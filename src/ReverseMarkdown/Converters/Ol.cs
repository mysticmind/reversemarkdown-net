using System;
using System.Linq;
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
            // Lists inside tables are not supported as markdown, so leave as HTML
            if (node.Ancestors("table").Count() > 0)
            {
                return node.OuterHtml;
            }

            return $"{Environment.NewLine}{TreatChildren(node)}{Environment.NewLine}";
		}
	}
}
