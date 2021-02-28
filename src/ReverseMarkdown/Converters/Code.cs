using HtmlAgilityPack;
using System.Net;
using System.Text;

namespace ReverseMarkdown.Converters
{
    public class Code : ConverterBase
    {
        public Code(Converter converter) : base(converter)
        {
            Converter.Register("code", this);
        }

        public override string Convert(HtmlNode node)
        {
            // HACK: Depending on the content "surrounding" the <code> element,
            // leading/trailing whitespace is significant. For example, the
            // following HTML renders as expected in a browser (meaning there is
            // proper spacing between words):
            //
            //   <p>The JavaScript<code> function </code>keyword...</p>
            //
            // However, if we simply trim the contents of the <code> element,
            // then the Markdown becomes:
            //
            //   The JavaScript`function`keyword...
            //
            // To avoid this scenario, if the <code> element contains leading
            // whitespace, add a space before the backquote Markdown character.
            // Likewise, if the <code> element contains trailing whitespace, add
            // a space after the backquote Markdown character.
            //
            // For the HTML example above, the Markdown will be:
            //
            //   The JavaScript `function` keyword...

            var sb = new StringBuilder();

            // Check if the <code> content has leading whitespace.
            if (node.InnerText != node.InnerText.TrimStart())
            {
                // The <code> content has leading whitespace.
                // Check if the conversion has already accounted for the
                // whitespace by processing the previous element.

                HtmlNode textNode = null;

                if (node.PreviousSibling != null
                    && node.PreviousSibling.Name == "#text")
                {
                    textNode = node.PreviousSibling;
                }

                // Check if the text in the previous node ends with whitespace.
                if (textNode != null
                    && textNode.InnerText != textNode.InnerText.TrimEnd())
                {
                    // The previous node ends with whitespace, so no need to
                    // add a space before the <code> content.
                }
                else
                {
                    // Add a space to separate the <code> content from the
                    // previous content.
                    sb.Append(' ');
                }
            }

            sb.Append('`');
            sb.Append(WebUtility.HtmlDecode(node.InnerText.Trim()));
            sb.Append('`');

            // Check if the <code> content has trailing whitespace.
            if (node.InnerText != node.InnerText.TrimEnd())
            {
                // The <code> content has trailing whitespace.
                // Check if the conversion will account for the
                // whitespace by processing the next element.

                HtmlNode textNode = null;

                if (node.NextSibling != null
                    && node.NextSibling.Name == "#text")
                {
                    textNode = node.NextSibling;
                }

                // Check if the text in the next node starts with whitespace.
                if (textNode != null
                    && textNode.InnerText != textNode.InnerText.TrimStart())
                {
                    // The next node starts with whitespace, so no need to
                    // add a space after the <code> content.
                }
                else
                {
                    // Add a space to separate the <code> content from the
                    // content that follows it.
                    sb.Append(' ');
                }
            }

            return sb.ToString();
        }
    }
}
