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
            // Depending on the content "surrounding" the <code> element,
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
            // To avoid this scenario, do *not* trim the inner text of the
            // <code> element.
            //
            // For the HTML example above, the Markdown will be:
            //
            //   The JavaScript` function `keyword...
            //
            // While it might seem preferable to try to "fix" this by trimming
            // the <code> element and insert leading/trailing spaces as
            // necessary, things become complicated rather quickly depending
            // on the particular content. For example, what would be the
            // "correct" conversion of the following HTML?
            //
            //   <p>The JavaScript<b><code> function </code></b>keyword...</p>
            //
            // The simplest conversion to Markdown:
            //
            //   The JavaScript**` function `**keyword...
            //
            // Some other, arguably "better", alternatives (that would require
            // substantially more conversion logic):
            //
            //   The JavaScript** `function` **keyword...
            //
            //   The JavaScript **`function`** keyword...

            var sb = new StringBuilder();

            sb.Append('`');
            sb.Append(WebUtility.HtmlDecode(node.InnerText));
            sb.Append('`');

            return sb.ToString();
        }
    }
}
