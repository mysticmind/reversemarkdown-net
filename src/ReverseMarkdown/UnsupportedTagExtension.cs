using System;

namespace ReverseMarkdown
{
    public class UnsupportedTagException : Exception
    {
        internal UnsupportedTagException(string message) : base(message)
        {
        }
    }

    public class SlackUnsupportedTagException : UnsupportedTagException
    {
        internal SlackUnsupportedTagException(string tagName)
            : base($"<{tagName}> tags cannot be converted to Slack-flavored markdown")
        {
        }
    }
}