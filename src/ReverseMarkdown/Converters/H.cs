using HtmlAgilityPack;
using static System.Convert;
using static System.Environment;

namespace ReverseMarkdown.Converters
{
    public class H
        : ConverterBase
    {
        public H(Converter converter)
            : base(converter)
        {
            Converter.Register("h1", this);
            Converter.Register("h2", this);
            Converter.Register("h3", this);
            Converter.Register("h4", this);
            Converter.Register("h5", this);
            Converter.Register("h6", this);
        }

        public override string Convert(HtmlNode node)
        {
            var prefix = new string('#', ToInt32(node
                .Name
                .Substring(1)));

            return $"{NewLine}{prefix} {TreatChildren(node)}{NewLine}";
        }
    }
}