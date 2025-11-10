#if NETSTANDARD || NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;


#pragma warning disable IDE0130 // Namespace should match folder structure
namespace ReverseMarkdown;
#pragma warning restore IDE0130

internal static class NetStandardPolyfills {
    private static readonly Regex _newLineRegex = new(@"\r?\n", RegexOptions.Compiled);

    internal static string ReplaceLineEndings(this string text, string replacement)
    {
        return _newLineRegex.Replace(text, replacement);
    }

    internal static List<HtmlNode>? GetValueOrDefault(this Dictionary<string, List<HtmlNode>> dict, string key)
    {
        if (dict.TryGetValue(key, out var nodes)) return nodes;
        return null;
    }

    internal static bool StartsWith(this string str, char value)
    {
        if (string.IsNullOrEmpty(str)) return false;
        return str[0] == value;
    }

    internal static bool EndsWith(this string str, char value)
    {
        return str.EndsWith(value.ToString());
    }

    internal static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> enumerable, Func<T, TKey> keySelector)
    {
        return enumerable.GroupBy(keySelector).Select(grp => grp.First());
    }
}
#endif
