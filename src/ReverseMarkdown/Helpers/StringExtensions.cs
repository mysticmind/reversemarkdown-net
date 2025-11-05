using System;
using System.Text.RegularExpressions;


namespace ReverseMarkdown.Helpers;

public static class StringExtensions {
    public static string Chomp(this string content)
    {
        return content
            .ReplaceLineEndings(string.Empty)
            .Trim();
    }

    internal static LineSplitEnumerator ReadLines(this string content)
    {
        return new LineSplitEnumerator(content);
    }

    internal static string Replace(this string content, StringReplaceValues replacements)
    {
        return replacements.Replace(content);
    }

    public static string FixMultipleNewlines(this string markdown)
    {
        var normalizedMarkdown = markdown.ReplaceLineEndings(Environment.NewLine);
        return Regex.Replace(normalizedMarkdown, $"{Environment.NewLine}{{2,}}", Environment.NewLine + Environment.NewLine);
    }
}
