using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace ReverseMarkdown.Helpers;

public static partial class StringUtils {
    /// <summary>
    /// <para>Gets scheme for provided uri string to overcome different behavior between windows/linux. https://github.com/dotnet/corefx/issues/1745</para>
    /// Assume http for url starting with //
    /// <para>Assume file for url starting with /</para>
    /// Otherwise give what <see cref="Uri.Scheme" /> gives us.
    /// <para>If non parseable by Uri, return empty string. Will never return null</para>
    /// </summary>
    /// <returns></returns>
    public static string GetScheme(string url)
    {
        //IETF RFC 3986
        if (Regex.IsMatch(url, "^//[^/]")) {
            return "http";
        }
        //Unix style path
        else if (Regex.IsMatch(url, "^/[^/]")) {
            return "file";
        }
        else if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
            return uri.Scheme;
        }
        else {
            return string.Empty;
        }
    }


    [GeneratedRegex(@"\r?\n\s*\r?\n", RegexOptions.Singleline)]
    private static partial Regex LinkTextRegex();

    private static readonly StringReplaceValues _linkTextReplaceValues = new() {
        ["["] = @"\[",
        ["]"] = @"\]",
    };

    /// <summary>
    /// Escape/clean characters which would break the [] section of a markdown []() link
    /// </summary>
    public static string EscapeLinkText(string rawText)
    {
        return LinkTextRegex()
            .Replace(rawText, Environment.NewLine)
            .Replace(_linkTextReplaceValues);
    }


    private static readonly Dictionary<string, string> EmptyStyles = new();

    public static Dictionary<string, string> ParseStyle(string? style)
    {
        if (string.IsNullOrEmpty(style)) {
            return EmptyStyles;
        }

        var styles = style!.Split(';');
        return styles.Select(styleItem => styleItem.Split(':'))
            .Where(styleParts => styleParts.Length == 2)
            .DistinctBy(styleParts => styleParts[0])
            .ToDictionary(styleParts => styleParts[0].Trim(), styleParts => styleParts[1].Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
