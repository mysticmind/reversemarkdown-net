using HtmlAgilityPack;
using ReverseMarkdown.Converters;
using System.IO;


namespace ReverseMarkdown.Test.Children {
    internal class IgnoreAWhenHasClass(Converter converter) : A(converter) {
        private const string Ignore = "ignore";

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            if (node.HasClass(Ignore))
                return;

            base.Convert(writer, node);
        }
    }
}
