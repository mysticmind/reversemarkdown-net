using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public class Text : ConverterBase
    {
        private readonly Dictionary<string, string> _escapedKeyChars = new Dictionary<string, string>();

        public Text(Converter converter) : base(converter)
        {
            _escapedKeyChars.Add("*", @"\*");
            _escapedKeyChars.Add("_", @"\_");

            Converter.Register("#text", this);
        }

        public override string Convert(HtmlNode node)
        {
            return node.InnerText == string.Empty ? TreatEmpty(node) : TreatText(node);
        }

        private string TreatText(HtmlNode node)
        {
            // Prevent &lt; and &gt; from being converted to < and > as this will be interpreted as HTML by markdown
            string content = node.InnerText
                .Replace("&lt;", "%3C")
                .Replace("&gt;", "%3E");

            content = DecodeHtml(content);

            // Not all renderers support hex encoded characters, so convert back to escaped HTML
            content = content
                .Replace("%3C", "&lt;")
                .Replace("%3E", "&gt;");

            //strip leading spaces and tabs for text within list item
            var parent = node.ParentNode;

            switch (parent.Name)
            {
                case "table":
                case "thead":
                case "tbody":    
                case "ol":
                case "ul":
                case "th":    
                case "tr":
                    content = content.Trim();
                    break;
            }

            if (parent.Ancestors("th").Any() || parent.Ancestors("td").Any())
            {
                content = ReplaceNewlineChars(parent, content);    
            }
            
            if (parent.Name != "a" && !Converter.Config.SlackFlavored)
            {
                content =  EscapeKeyChars(content);
            }

            content = PreserveKeyCharsWithinBackTicks(content);

            return content;
        }

        private string EscapeKeyChars(string content)
        {
            foreach(var item in _escapedKeyChars)
            {
                content = content.Replace(item.Key, item.Value);
            }

            return content;
        }

        private static string TreatEmpty(HtmlNode node)
        {
            var content = "";

            var parent = node.ParentNode;

            if (parent.Name == "ol" || parent.Name == "ul")
            {
                content = "";
            }
            else if(node.InnerText == " ")
            {
                content = " ";
            }

            return content;
        }

        private static string PreserveKeyCharsWithinBackTicks(string content)
        {
            var rx = new Regex("`.*?`");

            content = rx.Replace(content, p => p.Value.Replace(@"\*", "*").Replace(@"\_", "_"));

            return content;
        }

        private static string ReplaceNewlineChars(HtmlNode parentNode, string content)
        {
            if (parentNode.Name != "p" && parentNode.Name != "#document") return content;

            content = content.Replace("\r\n", "<br>");
            content = content.Replace("\n", "<br>");

            return content;
        }
    }
}
