using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using static System.String;

namespace ReverseMarkdown.Converters
{
    public class Text
        : ConverterBase
    {
        private readonly Dictionary<string, string> _escapedKeyChars = new Dictionary<string, string>();

        public Text(Converter converter)
            : base(converter)
        {
            /*
			this._escapedKeyChars.Add("\\",@"\\");
			this._escapedKeyChars.Add("`",@"\`");
			this._escapedKeyChars.Add("*",@"\*");
			this._escapedKeyChars.Add("_",@"\_");
			this._escapedKeyChars.Add("{",@"\{");
			this._escapedKeyChars.Add("}",@"\}");
			this._escapedKeyChars.Add("[",@"\[");
			this._escapedKeyChars.Add("]",@"\]");
			this._escapedKeyChars.Add("(",@"\)");
			this._escapedKeyChars.Add("#",@"\#");
			this._escapedKeyChars.Add("+",@"\+");
			this._escapedKeyChars.Add("-",@"\-");
			this._escapedKeyChars.Add(".",@"\.");
			this._escapedKeyChars.Add("!",@"\!");
			 */

            _escapedKeyChars.Add("*", @"\*");
            _escapedKeyChars.Add("_", @"\_");

            Converter.Register("#text", this);
        }

        public override string Convert(HtmlNode node)
        {
            return node.InnerText.Trim()?.Length == 0 ? TreatEmpty(node) : TreatText(node);
        }

        private static string TreatEmpty(HtmlNode node)
        {
            var parent = node.ParentNode;
            if (parent.Name == "ol" || parent.Name == "ul")
                return Empty;

            return node.InnerText == " " ? " " : Empty;
        }

        private string TreatText(HtmlNode node)
        {
            var content = DecodeHtml(node.InnerText)
                .Replace("\r", Empty)
                .Replace("\n", Empty);

            //strip leading spaces and tabs for text within list item
            var parent = node.ParentNode;
            if (parent.Name == "ol" || parent.Name == "ul")
                content = content.Trim();

            content = EscapeKeyChars(content);

            content = PreserveKeyCharswithinBackTicks(content);

            return content;
        }

        private string EscapeKeyChars(string content)
        {
            return _escapedKeyChars.Aggregate(
                content,
                (current, item) => current.Replace(item.Key, item.Value));
        }

        private static string PreserveKeyCharswithinBackTicks(string content)
        {
            var regex = new Regex("`.*?`", RegexOptions.Compiled);

            content = regex.Replace(content, p => p
                .Value
                .Replace(@"\*", "*")
                .Replace(@"\_", "_"));

            return content;
        }
    }
}