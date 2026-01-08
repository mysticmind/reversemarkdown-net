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

    /// <summary>
    /// Compacts HTML by removing line breaks and collapsing whitespace between HTML tags only,
    /// while preserving spaces within tag content. This is useful for nested tables in markdown.
    /// </summary>
    /// <param name="html">The HTML string to compact</param>
    /// <returns>Compacted HTML string suitable for embedding in markdown tables</returns>
    public static string CompactHtmlForMarkdown(this string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        // First remove all line endings
        html = html.ReplaceLineEndings("");

        // Use regex to collapse multiple spaces between tags (>...< patterns)
        // This preserves spaces within tag content
        html = Regex.Replace(html, @">\s+<", "> <");

        // Also trim any leading/trailing whitespace
        return html.Trim();
    }
}
