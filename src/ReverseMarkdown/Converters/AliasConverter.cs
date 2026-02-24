using System.IO;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters {
    public sealed class AliasConverter : ConverterBase {
        private readonly string _targetTag;

        public AliasConverter(Converter converter, string targetTag) : base(converter)
        {
            _targetTag = targetTag;
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (string.IsNullOrWhiteSpace(_targetTag)) {
                writer.Write(TreatChildrenAsString(node));
                return;
            }

            if (string.Equals(node.Name, _targetTag, System.StringComparison.OrdinalIgnoreCase) ||
                Context.AncestorsAny(node.Name)) {
                writer.Write(TreatChildrenAsString(node));
                return;
            }

            var target = Converter.Lookup(_targetTag);
            target.Convert(writer, node);
        }
    }
}
