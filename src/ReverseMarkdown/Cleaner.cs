using static System.Environment;
using static System.String;

namespace ReverseMarkdown
{
    public class Cleaner
    {
        private static string CleanTagBorders(string content)
        {
            // content from some htl editors such as CKEditor emits newline and tab between tags, clean that up

            return content
                .Replace("\n\t", Empty)
                .Replace(Format("{0}\t", NewLine), Empty);
        }

        public string PreTidy(string content)
        {
            return CleanTagBorders(content);
        }
    }
}