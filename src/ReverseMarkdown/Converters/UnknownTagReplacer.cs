using System.IO;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters {
    internal sealed class UnknownTagReplacer : ConverterBase {
        private readonly string _replacement;
        private readonly string _tagName;

        public UnknownTagReplacer(Converter converter, string tagName, string replacement) : base(converter)
        {
            _tagName = tagName;
            _replacement = replacement;
        }

        internal string Replacement => _replacement;

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            var content = TreatChildrenAsString(node);

            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrEmpty(_replacement) ||
                Context.AncestorsAny(_tagName)) {
                writer.Write(content);
                return;
            }

            var spaceSuffix = node.NextSibling?.Name == _tagName ? " " : string.Empty;
            TreatEmphasizeContentWhitespaceGuard(writer, content, _replacement, spaceSuffix);
        }
    }
}
