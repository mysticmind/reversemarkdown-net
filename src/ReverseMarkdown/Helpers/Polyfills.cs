#if NETSTANDARD2_0 || NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReverseMarkdown.Compat;

/// <summary>
/// Polyfills for BCL APIs that exist on net6.0+/netstandard2.1 but are missing on
/// netstandard2.0 and .NET Framework (net46). These are surfaced everywhere on those
/// targets via a global using (see GlobalUsings.cs) so the main code can use the modern
/// instance methods unchanged. On net6.0+ targets this file is compiled out and the BCL
/// implementations are used instead.
/// </summary>
internal static class Polyfills {
    // string.ReplaceLineEndings(string) — added in .NET 6. Mirrors the BCL's recognised
    // line-break set (CR, LF, CRLF, FF, NEL, LS, PS), treating CRLF as a single sequence.
    private static readonly Regex LineEndings =
        new(@"\r\n|[\r\n\f\u0085\u2028\u2029]", RegexOptions.Compiled);

    internal static string ReplaceLineEndings(this string text, string replacementText)
    {
        return LineEndings.Replace(text, replacementText);
    }

    // string.Contains(char) — added in netstandard2.1.
    internal static bool Contains(this string text, char value)
    {
        return text.IndexOf(value) >= 0;
    }

    // string.Contains(string, StringComparison) — added in netstandard2.1.
    internal static bool Contains(this string text, string value, StringComparison comparisonType)
    {
        return text.IndexOf(value, comparisonType) >= 0;
    }

    // string.StartsWith(char) — added in netstandard2.1.
    internal static bool StartsWith(this string text, char value)
    {
        return text.Length > 0 && text[0] == value;
    }

    // string.EndsWith(char) — added in netstandard2.1.
    internal static bool EndsWith(this string text, char value)
    {
        return text.Length > 0 && text[text.Length - 1] == value;
    }

    // Enumerable.DistinctBy(..) — added in .NET 6.
    internal static IEnumerable<TSource> DistinctBy<TSource, TKey>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        return source.GroupBy(keySelector, comparer).Select(group => group.First());
    }

    // CollectionExtensions.GetValueOrDefault(..) — added in netstandard2.1.
    internal static TValue? GetValueOrDefault<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary, TKey key)
    {
        return dictionary.TryGetValue(key, out var value) ? value : default;
    }
}
#endif
