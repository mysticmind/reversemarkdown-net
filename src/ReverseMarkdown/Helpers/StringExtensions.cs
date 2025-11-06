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

#if NET6_0_OR_GREATER
    internal static LineSplitEnumerator ReadLines(this string content)
    {
        return new LineSplitEnumerator(content);
    }
#else
    internal static System.Collections.Generic.IEnumerable<string> ReadLines(this string content)
    {
        var reader = new System.IO.StringReader(content);
        while (reader.ReadLine() is { } line) {
            yield return line;
        }
    }
#endif

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
