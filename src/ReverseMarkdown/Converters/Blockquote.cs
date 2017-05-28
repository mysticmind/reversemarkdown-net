using System.Linq;
using HtmlAgilityPack;
using static System.Environment;
using static System.String;

namespace ReverseMarkdown.Converters
{
    public class Blockquote
        : ConverterBase
    {
        public Blockquote(Converter converter)
            : base(converter)
        {
            Converter.Register("blockquote", this);
        }

        public override string Convert(HtmlNode node)
        {
            // get the lines based on carriage return and prefix "> " to each line
            var lines = TreatChildren(node)
                .Trim()
                .ReadLines()
                .Select(item => $"> {item}{NewLine}");

            // join all the lines to a single line
            return $"{NewLine}{NewLine}{lines.Aggregate(Empty, (curr, next) => curr + next)}{NewLine}";
        }
    }
}