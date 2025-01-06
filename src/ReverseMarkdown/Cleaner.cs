
using System;
using System.Text.RegularExpressions;

namespace ReverseMarkdown
{
    public static class Cleaner
    {
        private static readonly Regex SlackBoldCleaner = new Regex(@"\*(\s\*)+");
        private static readonly Regex SlackItalicCleaner = new Regex(@"_(\s_)+");
        
        private static string CleanTagBorders(string content)
        {
            // content from some htl editors such as CKEditor emits newline and tab between tags, clean that up
            content = content.Replace("\n\t", "");
            content = content.Replace(Environment.NewLine + "\t", "");
            return content;
        }

        private static string NormalizeSpaceChars(string content)
        {
            // replace unicode and non-breaking spaces to normal space
            content = Regex.Replace(content, @"[\u0020\u00A0]", " ");
            return content;
        }

        public static string PreTidy(string content, bool removeComments)
        {
            content = NormalizeSpaceChars(content);
            content = CleanTagBorders(content);

            return content;
        }

        public static string SlackTidy(string content)
        {
            // Slack's escaping rules depend on whether the key characters appear in
            // next to word characters or not.
            content = SlackBoldCleaner.Replace(content, "*");
            content = SlackItalicCleaner.Replace(content, "_");
            
            return content;
        }
    }
}
