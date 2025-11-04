using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters
{
    public partial class Text : ConverterBase
    {
        public Text(Converter converter) : base(converter)
        {
            Converter.Register("#text", this);
        }

        public override string Convert(HtmlNode node)
        {
            return node.InnerText is "" or " " or "&nbsp;" ? TreatEmpty(node) : TreatText(node);
        }

        private static readonly StringReplaceValues _escapedKeyChars = new(new() {
            ["*"] = @"\*",
            ["_"] = @"\_",
        });

        private static readonly StringReplaceValues _escapedKeyCharsReverse = new(new() {
            [@"\*"] = "*",
            [@"\_"] = "_",
        });

        private static readonly StringReplaceValues _specialMarkdownCharacters = new(new() {
            ["["] = @"\[",
            ["]"] = @"\]",
            ["("] = @"\(",
            [")"] = @"\)",
            ["{"] = @"\{",
            ["}"] = @"\}",
        });

        private static readonly StringReplaceValues _preserveAngleBrackets = new(new() {
            ["&lt;"] = "%3C",
            ["&gt;"] = "%3E",
        });

        private static readonly StringReplaceValues _unPreserveAngleBrackets = new(new() {
            ["%3C"] = "&lt;",
            ["%3E"] = "&gt;",
        });

        [GeneratedRegex(@"`.*?`")]
        private static partial Regex BackTicks { get; }


        private string TreatText(HtmlNode node)
        {
            // Prevent &lt; and &gt; from being converted to < and > as this will be interpreted as HTML by Markdown
            string content = node.InnerText.Replace(_preserveAngleBrackets);

            content = DecodeHtml(content);

            // Not all renderers support hex encoded characters, so convert back to escaped HTML
            content = content.Replace(_unPreserveAngleBrackets);

            //strip leading spaces and tabs for text within a list item
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

            if (
                (Context.AncestorsAny("th") || Context.AncestorsAny("td")) && // O(1) do fast check before going for full check
                (parent.Ancestors("th").Any() || parent.Ancestors("td").Any()) // O(n)
            ) {
                content = ReplaceNewlineChars(parent, content);    
            }
            
            if (parent.Name != "a" && !Converter.Config.SlackFlavored)
            {
                content =  EscapeKeyChars(content);
            }

            content = PreserveKeyCharsWithinBackTicks(content);
            content = EscapeSpecialMarkdownCharacters(content);

            return content;
        }

        private string EscapeKeyChars(string content)
        {
            return content.Replace(_escapedKeyChars);
        }

        private static string TreatEmpty(HtmlNode node)
        {
            var content = string.Empty;

            if (node.ParentNode.Name is "ol" or "ul")
            {
                content = string.Empty;
            }
            else if(node.InnerText is " " or "&nbsp;")
            {
                content = " ";
            }

            return content;
        }

        private string PreserveKeyCharsWithinBackTicks(string content)
        {
            content = BackTicks.Replace(content, p => p.Value.Replace(_escapedKeyCharsReverse));
            return content;
        }

        private static string ReplaceNewlineChars(HtmlNode parentNode, string content)
        {
            if (parentNode.Name is "p" or "#document") {
                content = content.ReplaceLineEndings("<br>");
            }

            return content;
        }

        private static bool IsContentWithinBackTicks(string content)
        {
            return content.StartsWith('`') && content.EndsWith('`');
        }

        private string EscapeSpecialMarkdownCharacters(string content)
        {
            if (IsContentWithinBackTicks(content))
            {
                return content;
            }
            
            return content.Replace(_specialMarkdownCharacters);
        }

    }
}
