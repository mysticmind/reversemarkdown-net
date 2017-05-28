using HtmlAgilityPack;
using static System.Environment;

namespace ReverseMarkdown.Converters
{
    public class Table
        : ConverterBase
    {
        public Table(Converter converter)
            : base(converter)
        {
            Converter.Register("table", this);
        }

        public override string Convert(HtmlNode node)
        {
            return $"{NewLine}{NewLine}{TreatChildren(node)}{NewLine}";
        }
    }
}