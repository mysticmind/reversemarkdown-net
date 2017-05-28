﻿using HtmlAgilityPack;
using static System.Environment;
using static System.String;

namespace ReverseMarkdown.Converters
{
    public class Br
        : ConverterBase
    {
        public Br(Converter converter)
            : base(converter)
        {
            Converter.Register("br", this);
        }

        public override string Convert(HtmlNode node)
        {
            return Converter.Config.GithubFlavored
                ? NewLine
                : Format("  {0}", NewLine);
        }
    }
}