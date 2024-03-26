using HtmlAgilityPack;

using ReverseMarkdown.Converters;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseMarkdown.Test.Children
{
    internal class IgnoreAWhenHasClass : A
    {
        private readonly string _ignore = "ignore";

        public IgnoreAWhenHasClass(Converter converter) : base(converter)
        { }

        public override string Convert(HtmlNode node)
        {
            if (node.HasClass(_ignore))
                return "";

            return base.Convert(node);
        }
    }
}
