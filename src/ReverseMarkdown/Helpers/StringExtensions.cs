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
    // LineSplitEnumerator relies on ReadOnlySpan<char>, which we avoid on netstandard2.0/net46.
    // StringReader.ReadLine splits on \r, \n and \r\n with identical line semantics.
    internal static System.Collections.Generic.IEnumerable<string> ReadLines(this string content)
    {
        using var reader = new System.IO.StringReader(content);
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

    public static string CompactHtmlForCommonMarkBlock(this string html)
    {
        if (string.IsNullOrEmpty(html)) {
            return html;
        }

        html = html.ReplaceLineEndings("\n");

        html = Regex.Replace(html, @"<pre><code>(.*?)</code></pre>", match =>
        {
            var content = match.Groups[1].Value.Replace("\n", "&#10;");
            return $"<pre><code>{content}</code></pre>";
        }, RegexOptions.Singleline);

        html = Regex.Replace(html, @">\s+<", "><");

        return html.Trim();
    }
}
