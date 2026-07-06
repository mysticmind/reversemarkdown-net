using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace ReverseMarkdown.Helpers;

public static partial class StringUtils {
    private static readonly HashSet<char> TelegramMarkdownV2EscapableChars = new() {
        '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!', '\\'
    };

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


#if NET7_0_OR_GREATER
    [GeneratedRegex(@"\r?\n\s*\r?\n", RegexOptions.Singleline)]
    private static partial Regex LinkTextRegex();

    [GeneratedRegex(@"`+")]
    private static partial Regex BackTickRunRegex();
#else
    private static readonly Regex _linkTextRegex =
        new(@"\r?\n\s*\r?\n", RegexOptions.Singleline | RegexOptions.Compiled);
    private static Regex LinkTextRegex() => _linkTextRegex;

    private static readonly Regex _backTickRunRegex = new(@"`+", RegexOptions.Compiled);
    private static Regex BackTickRunRegex() => _backTickRunRegex;
#endif

    private static readonly StringReplaceValues _linkTextReplaceValues = new() {
        ["["] = @"\[",
        ["]"] = @"\]",
    };

    /// <summary>
    /// Escape/clean characters which would break the [] section of a markdown []() link.
    /// Square brackets inside inline code spans (delimited by backticks) are left untouched,
    /// since they are not interpreted as link delimiters there.
    /// </summary>
    public static string EscapeLinkText(string rawText)
    {
        var text = LinkTextRegex().Replace(rawText, Environment.NewLine);
        return EscapeBracketsOutsideCodeSpans(text);
    }

    private static string EscapeBracketsOutsideCodeSpans(string text)
    {
        if (!text.Contains('`')) {
            return text.Replace(_linkTextReplaceValues);
        }

        var builder = new StringBuilder(text.Length);
        var index = 0;
        while (index < text.Length) {
            var openMatch = BackTickRunRegex().Match(text, index);
            if (!openMatch.Success) {
                builder.Append(text.Substring(index).Replace(_linkTextReplaceValues));
                break;
            }

            // Escape brackets in the text preceding the code span.
            builder.Append(text.Substring(index, openMatch.Index - index).Replace(_linkTextReplaceValues));

            // Find the matching closing backtick run of the same length.
            var closeMatch = openMatch.NextMatch();
            while (closeMatch.Success && closeMatch.Value.Length != openMatch.Value.Length) {
                closeMatch = closeMatch.NextMatch();
            }

            if (!closeMatch.Success) {
                // Unbalanced backticks: treat the rest as ordinary text and escape it.
                builder.Append(text.Substring(openMatch.Index).Replace(_linkTextReplaceValues));
                break;
            }

            // Copy the code span (including delimiters) verbatim.
            var spanEnd = closeMatch.Index + closeMatch.Length;
            builder.Append(text, openMatch.Index, spanEnd - openMatch.Index);
            index = spanEnd;
        }

        return builder.ToString();
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
            .Select(styleParts => new[] { styleParts[0].Trim(), styleParts[1].Trim() })
            .DistinctBy(styleParts => styleParts[0], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(styleParts => styleParts[0], styleParts => styleParts[1], StringComparer.OrdinalIgnoreCase);
    }

    public static string EscapeTelegramMarkdownV2(string content)
    {
        return EscapeChars(content, c => TelegramMarkdownV2EscapableChars.Contains(c));
    }

    public static string EscapeTelegramMarkdownV2Code(string content)
    {
        return EscapeChars(content, c => c is '`' or '\\');
    }

    public static string EscapeTelegramMarkdownV2LinkUrl(string url)
    {
        return EscapeChars(url, c => c is ')' or '\\');
    }

    private static string EscapeChars(string content, Func<char, bool> mustEscape)
    {
        if (string.IsNullOrEmpty(content)) {
            return content;
        }

        var builder = new StringBuilder(content.Length + 16);
        foreach (var c in content) {
            if (mustEscape(c)) {
                builder.Append('\\');
            }

            builder.Append(c);
        }

        return builder.ToString();
    }
}
