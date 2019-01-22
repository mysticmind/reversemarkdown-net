using HtmlAgilityPack;
using System;

namespace ReverseMarkdown.Converters {
    public class A : ConverterBase
    {
        public A(Converter converter)
            : base(converter)
        {
            Converter.Register("a", this);
        }

        public override string Convert(HtmlNode node)
        {
            var name = TreatChildren(node).Trim();

            var href = node.GetAttributeValue("href", string.Empty).Trim();
            var title = ExtractTitle(node);
            title = title.Length > 0 ? $" \"{title}\"" : "";
            var scheme = LinkParser.GetScheme(href);
            
            var isRemoveLinkWhenSameName = Converter.Config.HrefHandling == Config.HrefHandlingOption.Smart
                                           && scheme != string.Empty
                                           && Uri.IsWellFormedUriString(href, UriKind.RelativeOrAbsolute)
                                           && (
                                               href.Equals(name, StringComparison.OrdinalIgnoreCase)
                                                || href.Equals($"tel:{name}", StringComparison.OrdinalIgnoreCase)
                                                || href.Equals($"mailto:{name}", StringComparison.OrdinalIgnoreCase)
                                            );

            if (href.StartsWith("#") //anchor link
                || !Converter.Config.IsSchemeWhitelisted(scheme) //Not allowed scheme
                || isRemoveLinkWhenSameName //Same link - why bother with [](). Except when incorrectly escaped, i.e unescaped spaces - then bother with []()
                || string.IsNullOrEmpty(href) //We would otherwise print empty () here...
                || string.IsNullOrEmpty(name))
			{
				return name;
			}
			else {
                var useHrefWithHttpWhenNameHasNoScheme = Converter.Config.HrefHandling == Config.HrefHandlingOption.Smart &&
                                                        (scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                                                        && string.Equals(href, $"{scheme}://{name}", StringComparison.OrdinalIgnoreCase);

                return useHrefWithHttpWhenNameHasNoScheme ? href : $"[{name}]({href}{title})";
            }
		}
	}
}
