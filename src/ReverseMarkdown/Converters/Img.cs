using HtmlAgilityPack;
using System;

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

            var scheme = "";
            bool isRelativeUrl = false;
            try {
                scheme = (new Uri(src, UriKind.Absolute)).Scheme;
            }
            catch {
                isRelativeUrl = true;
            }

            if (!isRelativeUrl && !Converter.Config.IsSchemeAllowed(scheme)) { return ""; }

            string title = this.ExtractTitle(node);
            title = title.Length > 0 ? $" \"{title}\"" : "";

            return $"![{alt}]({src}{title})";
        }
    }
}
