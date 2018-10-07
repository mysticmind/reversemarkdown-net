
using System;
using System.Text.RegularExpressions;

namespace ReverseMarkdown
{
    public class Cleaner
    {
        private static string CleanTagBorders(string content)
        {
            // content from some htl editors such as CKEditor emits newline and tab between tags, clean that up
            content = content.Replace("\n\t", "");
            content = content.Replace(Environment.NewLine + "\t", "");
            return content;
        }

        private static string RemoveComments(string content)
        {
            // optionally remove HTML comment tags from content (i.e `<!-- this is a comment block -->`)
            content = Regex.Replace(content, @"<!--(\n|.)*-->", "");
            return content;
        }

        public static string PreTidy(string content, bool removeComments)
        {
            content = CleanTagBorders(content);

            if (removeComments)
            {
                content = RemoveComments(content);
            }

            return content;
        }
    }
}
