﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReverseMarkdown
{
    public static class StringUtils
    {
        public static string Chomp(this string content, bool all=false)
        {
            if (all)
            {
                return content
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Trim();
            }

            return content.Trim().TrimEnd('\r', '\n');
        }

        public static IEnumerable<string> ReadLines(this string content)
        {
            string line;
            using (var sr = new StringReader(content))
                while ((line = sr.ReadLine()) != null)
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
        public static string GetScheme(string url) {
            var isValidUri = Uri.TryCreate(url, UriKind.Absolute, out Uri uri);
            //IETF RFC 3986
            if (Regex.IsMatch(url, "^//[^/]")) {
                return "http";
            }
            //Unix style path
            else if (Regex.IsMatch(url, "^/[^/]")) {
                return "file";
            }
            else if (isValidUri) {
                return uri.Scheme;
            }
            else {
                return String.Empty;
            }
        }

        /// <summary>
        /// Escape/clean characters which would break the [] section of a markdown []() link
        /// </summary>
        public static string EscapeLinkText(string rawText)
        {
            return Regex.Replace(rawText, @"\r?\n\s*\r?\n", Environment.NewLine, RegexOptions.Singleline)
                .Replace("[", @"\[")
                .Replace("]", @"\]");
        }

        public static Dictionary<string, string> ParseStyle(string style)
        {
            if (string.IsNullOrEmpty(style))
            {
                return new Dictionary<string, string>();
            }

            var styles = style.Split(';');
            return styles.Select(styleItem => styleItem.Split(':'))
                .Where(styleParts => styleParts.Length == 2)
                .DistinctBy(styleParts => styleParts[0])
                .ToDictionary(styleParts => styleParts[0], styleParts => styleParts[1]);
        }
        
        private static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector)
        {
            return enumerable.GroupBy(keySelector).Select(grp => grp.First());
        }
    }
}
