using System;

namespace ReverseMarkdown
{
    public class UnknownTagException : Exception
    {
        public UnknownTagException(string tagName): base($"Unknown tag: {tagName}")
        {
        }
    }
}
