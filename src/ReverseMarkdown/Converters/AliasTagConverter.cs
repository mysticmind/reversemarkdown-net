using System.IO;
using HtmlAgilityPack;

namespace ReverseMarkdown.Converters {
    internal sealed class AliasTagConverter : ConverterBase {
        private readonly string _sourceTag;
        private readonly string _targetTag;

        public AliasTagConverter(Converter converter, string sourceTag, string targetTag) : base(converter)
        {
            _sourceTag = sourceTag;
            _targetTag = targetTag;
        }

        internal string TargetTag => _targetTag;

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (Context.AncestorsAny(_sourceTag)) {
                writer.Write(TreatChildrenAsString(node));
                return;
            }

            var target = Converter.Lookup(_targetTag);
            target.Convert(writer, node);
        }
    }
}
