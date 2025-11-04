using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReverseMarkdown
{
    public static class StringUtils
    {
        public static string Chomp(this string content, bool all=false)
        {
            // TODO optimize:
            if (all)
            {
                return content
                    .ReplaceLineEndings(string.Empty)
                    .Trim();
            }

            return content.Trim(); // trim also removes leading/trailing new lines
        }

        public static IEnumerable<string> ReadLines(this string content)
        {
            using var sr = new StringReader(content);
            while (sr.ReadLine() is { } line)
                yield return line;
        }

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
            else if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) {
                return uri.Scheme;
            }
            else {
                return string.Empty;
            }
        }

        /// <summary>
        /// Escape/clean characters which would break the [] section of a markdown []() link
        /// </summary>
        public static string EscapeLinkText(string rawText)
        {
            // TODO optimize:
            return Regex.Replace(rawText, @"\r?\n\s*\r?\n", Environment.NewLine, RegexOptions.Singleline)
                .Replace("[", @"\[")
                .Replace("]", @"\]");
        }

        private static readonly Dictionary<string, string> EmptyStyles = new();
        public static Dictionary<string, string> ParseStyle(string style)
        {
            if (string.IsNullOrEmpty(style))
            {
                return EmptyStyles;
            }

            var styles = style.Split(';');
            return styles.Select(styleItem => styleItem.Split(':'))
                .Where(styleParts => styleParts.Length == 2)
                .DistinctBy(styleParts => styleParts[0])
                .ToDictionary(styleParts => styleParts[0], styleParts => styleParts[1]);
        }
        
        public static int LeadingSpaceCount(this string content)
        {
            var leadingSpaces = 0;
            foreach (var c in content)
            {
                if (c == ' ')
                {
                    leadingSpaces++;
                }
                else
                {
                    break;
                }
            }
            return leadingSpaces;
        }
        
        public static int TrailingSpaceCount(this string content)
        {
            var trailingSpaces = 0;
            for (var i = content.Length - 1; i >= 0; i--)
            {
                if (content[i] == ' ')
                {
                    trailingSpaces++;
                }
                else
                {
                    break;
                }
            }
            return trailingSpaces;
        }

        public static string EmphasizeContentWhitespaceGuard(this string content, string emphasis, string nextSiblingSpaceSuffix="")
        {
            // TODO maybe optimize:
            var leadingSpaces = new string(' ', content.LeadingSpaceCount());
            var trailingSpaces = new string(' ', content.TrailingSpaceCount());

            return $"{leadingSpaces}{emphasis}{content.Chomp(all:true)}{emphasis}{(trailingSpaces.Length > 0 ? trailingSpaces : nextSiblingSpaceSuffix)}";
        }
        
        public static string FixMultipleNewlines(this string markdown)
        {
            var normalizedMarkdown = Regex.Replace(markdown, @"\r\n|\r|\n", Environment.NewLine);
            var pattern = $"{Environment.NewLine}{{2,}}";
            return Regex.Replace(normalizedMarkdown, pattern, Environment.NewLine + Environment.NewLine);
        }

        private static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector)
        {
            return enumerable.GroupBy(keySelector).Select(grp => grp.First());
        }
        

        internal static string Replace(this string content, StringReplaceValues replacements)
        {
            return replacements.Replace(content);
        }
    }
}

internal class StringReplaceValues {
    private readonly Dictionary<string, string> _replacements;
    private readonly Regex _regex;

    public StringReplaceValues(Dictionary<string, string> replacements)
    {
        _replacements = replacements;
        _regex = new Regex($"{string.Join("|", _replacements.Keys.Select(Regex.Escape))}");
    }

    public string Replace(string input)
    {
        var offset = 0;
        StringBuilder sb = null;
        foreach (var match in _regex.EnumerateMatches(input)) {
            sb ??= new StringBuilder(input.Length);
            sb.Append(input.AsSpan(offset, match.Index - offset));
            sb.Append(_replacements[input.AsSpan(match.Index, match.Length).ToString()]);
            offset = match.Index + match.Length;
        }

        if (sb is not null && offset != input.Length) {
            sb.Append(input.AsSpan(offset, input.Length - offset));
        }

        return sb?.ToString() ?? input;
    }
}