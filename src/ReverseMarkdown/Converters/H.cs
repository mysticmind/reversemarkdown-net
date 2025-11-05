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

            writer.WriteLine();
            writer.Write(new string('#', level));
            writer.Write(' ');
            TreatChildren(writer, node);
            writer.WriteLine();
        }
    }
}
