using HtmlAgilityPack;
using System;
using System.Text.RegularExpressions;

namespace ReverseMarkdown.Converters {
    public class Img : ConverterBase
    {
        public Img(Converter converter) : base(converter)
        {
            Converter.Register("img", this);
        }

		public override string Convert(HtmlNode node)
		{
			string alt = node.GetAttributeValue("alt", string.Empty);
			string src = node.GetAttributeValue("src", string.Empty);

            if (!Converter.Config.IsSchemeWhitelisted(LinkParser.GetScheme(src))) { return ""; }

            string title = this.ExtractTitle(node);
            title = title.Length > 0 ? $" \"{title}\"" : "";

            return $"![{alt}]({src}{title})";
        }
    }
}
