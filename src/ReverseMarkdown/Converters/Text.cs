using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown.Converters {
    public partial class Text : ConverterBase {
        public Text(Converter converter) : base(converter)
        {
            Converter.Register("#text", this);
        }


        #region values

        private static readonly StringReplaceValues _escapedKeyChars = new() {
            ["*"] = @"\*",
            ["_"] = @"\_",
        };

        private static readonly StringReplaceValues _escapedKeyCharsReverse = new() {
            [@"\*"] = "*",
            [@"\_"] = "_",
        };

        private static readonly StringReplaceValues _specialMarkdownCharacters = new() {
            ["["] = @"\[",
            ["]"] = @"\]",
            ["("] = @"\(",
            [")"] = @"\)",
            ["{"] = @"\{",
            ["}"] = @"\}",
        };

        private static readonly StringReplaceValues _preserveAngleBrackets = new() {
            ["&lt;"] = "%3C",
            ["&gt;"] = "%3E",
        };

        private static readonly StringReplaceValues _unPreserveAngleBrackets = new() {
            ["%3C"] = "&lt;",
            ["%3E"] = "&gt;",
        };

        [GeneratedRegex(@"`.*?`")]
        private static partial Regex BackTicks();

        #endregion


        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (node.InnerText is " " or "&nbsp;" && node.ParentNode.Name is not ("ol" or "ul")) {
                writer.Write(' ');
            }
            else {
                TreatText(writer, node);
            }
        }


        private void TreatText(TextWriter writer, HtmlNode node)
        {
            var text = node.InnerText;
            var parent = node.ParentNode;

            if (string.IsNullOrEmpty(text)) {
                return;
            }

            //strip leading spaces and tabs for text within a list item
            var shouldTrim = (
                parent.Name is "table" or "thead" or "tbody" or "ol" or "ul" or "th" or "tr"
            );
            var replaceLineEndings = (
                parent.Name is "p" or "#document" &&
                //(Context.AncestorsAny("th") || Context.AncestorsAny("td"))
                (parent.Ancestors("th").Any() || parent.Ancestors("td").Any())
            );

            // Prevent &lt; and &gt; from being converted to < and > as this will be interpreted as HTML by Markdown
            //var search = SearchValues.Create(["&lt;", "&gt;"], StringComparison.Ordinal);
            //var index = text.IndexOfAny(search);
            //if (index != -1) {
            //}

            // html decode:
            var content = text.Replace(_preserveAngleBrackets);
            content = DecodeHtml(content);
            content = content.Replace(_unPreserveAngleBrackets);

            if (shouldTrim) {
                content = content.Trim();
            }

            if (replaceLineEndings) {
                content = content.ReplaceLineEndings("<br>");
            }

            if (parent.Name != "a" && !Converter.Config.SlackFlavored) {
                content = content.Replace(_escapedKeyChars);
                // Preserve Key Chars Within BackTicks:
                content = BackTicks().Replace(content, p => p.Value.Replace(_escapedKeyCharsReverse));
            }

            content = EscapeSpecialMarkdownCharacters(content);

            writer.Write(content);
        }


        private static string EscapeSpecialMarkdownCharacters(string content)
        {
            return content.StartsWith('`') && content.EndsWith('`')
                ? content
                : content.Replace(_specialMarkdownCharacters);
        }
    }
}
