using System.IO;
using HtmlAgilityPack;


namespace ReverseMarkdown.Converters {
    public class H : ConverterBase {
        public H(Converter converter) : base(converter)
        {
            Converter.Register("h1", this);
            Converter.Register("h2", this);
            Converter.Register("h3", this);
            Converter.Register("h4", this);
            Converter.Register("h5", this);
            Converter.Register("h6", this);
        }

        public override void Convert(TextWriter writer, HtmlNode node)
        {
            // Headings inside tables are not supported as markdown, so just ignore the heading and convert children
            if (Context.AncestorsAny("table")) {
                TreatChildren(writer, node);
                return;
            }

            var level = node.Name[1] - '0'; // 'h1' -> 1, 'h2' -> 2, etc.

            var content = TreatChildrenAsString(node);
            if (Converter.Config.TelegramMarkdownV2) {
                writer.WriteLine();
                for (var i = 0; i < level; i++) {
                    writer.Write("\\#");
                }

                writer.Write(' ');
                writer.Write(content);
                writer.WriteLine();
                return;
            }

            if (Converter.Config.CommonMark) {
                content = content.ReplaceLineEndings("&#10;");
                content = EscapeTrailingHashes(content);
            }

            writer.WriteLine();
            writer.Write(new string('#', level));
            writer.Write(' ');
            writer.Write(content);
            writer.WriteLine();
        }

        private static string EscapeTrailingHashes(string content)
        {
            if (string.IsNullOrEmpty(content)) {
                return content;
            }

            var index = content.Length - 1;
            while (index >= 0 && content[index] == '#') {
                index--;
            }

            if (index == content.Length - 1) {
                return content;
            }

            var hashCount = content.Length - 1 - index;
            var escapedHashes = new string('#', hashCount);
            escapedHashes = escapedHashes.Replace("#", "\\#");
            return content[..(index + 1)] + escapedHashes;
        }
    }
}
