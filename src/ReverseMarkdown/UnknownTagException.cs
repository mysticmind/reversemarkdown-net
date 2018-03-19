using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReverseMarkdown
{
    public class UnknownTagException : Exception
    {
        public UnknownTagException(string tagName): base($"Unknown tag: {tagName}")
        {
        }
    }
}
