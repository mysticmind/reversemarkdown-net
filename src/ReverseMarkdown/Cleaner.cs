using System;
using System.Text.RegularExpressions;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown;

public static partial class Cleaner {
#if NET7_0_OR_GREATER
    [GeneratedRegex(@"\*(\s\*)+")]
    private static partial Regex SlackBoldCleaner();

    [GeneratedRegex(@"_(\s_)+")]
    private static partial Regex SlackItalicCleaner();

    [GeneratedRegex(@"[\u0020\u00A0]")]
    private static partial Regex NonBreakingSpaces();

    // Cached: these were rebuilt on every conversion, four Regex constructions per Convert call.
    [GeneratedRegex(@"<\s*script\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptOpen();

    [GeneratedRegex(@"</\s*script\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptClose();

    [GeneratedRegex(@"<\s*style\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleOpen();

    [GeneratedRegex(@"</\s*style\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex StyleClose();
#else
    private static readonly Regex _slackBoldCleaner = new(@"\*(\s\*)+", RegexOptions.Compiled);
    private static Regex SlackBoldCleaner() => _slackBoldCleaner;

    private static readonly Regex _slackItalicCleaner = new(@"_(\s_)+", RegexOptions.Compiled);
    private static Regex SlackItalicCleaner() => _slackItalicCleaner;

    private static readonly Regex _nonBreakingSpaces = new(@"[  ]", RegexOptions.Compiled);
    private static Regex NonBreakingSpaces() => _nonBreakingSpaces;

    private static readonly Regex _scriptOpen = new(@"<\s*script\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static Regex ScriptOpen() => _scriptOpen;

    private static readonly Regex _scriptClose = new(@"</\s*script\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static Regex ScriptClose() => _scriptClose;

    private static readonly Regex _styleOpen = new(@"<\s*style\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static Regex StyleOpen() => _styleOpen;

    private static readonly Regex _styleClose = new(@"</\s*style\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static Regex StyleClose() => _styleClose;
#endif

    private static readonly StringReplaceValues TagBorders = new() {
        ["\n\t"] = string.Empty,
        ["\r\n\t"] = string.Empty,
    };

    private static string CleanTagBorders(string content)
    {
        // content from some htl editors such as CKEditor emits newline and tab between tags, clean that up
        return content.Replace(TagBorders);
    }

    private static string NormalizeSpaceChars(string content)
    {
        // replace unicode and non-breaking spaces to normal space
        content = NonBreakingSpaces().Replace(content, " ");
        return content;
    }

    public static string PreTidy(string content, bool removeComments)
    {
        content = NormalizeSpaceChars(content);
        content = FixUnclosedTag(content, ScriptOpen(), ScriptClose());
        content = FixUnclosedTag(content, StyleOpen(), StyleClose());
        content = CleanTagBorders(content);

        return content;
    }

    /// <summary>Strip unclosed &lt;script&gt;/&lt;style&gt; open tags before parsing. Left in place,
    /// the HTML5 parser consumes the rest of the document as their raw-text content and the real
    /// markup is lost. This is the minimal pre-parse fixup that does not touch text content (unlike
    /// <see cref="PreTidy"/>), so it is safe on the CommonMark round-trip path.</summary>
    public static string FixUnclosedScriptStyle(string content)
    {
        content = FixUnclosedTag(content, ScriptOpen(), ScriptClose());
        content = FixUnclosedTag(content, StyleOpen(), StyleClose());
        return content;
    }

    private static string FixUnclosedTag(string content, Regex openRegex, Regex closeRegex)
    {
        if (string.IsNullOrWhiteSpace(content)) {
            return content;
        }

        // Nothing to do for the overwhelmingly common case, and it avoids the match machinery
        // entirely on documents with no such tag.
        if (!openRegex.IsMatch(content)) {
            return content;
        }

        return openRegex.Replace(content, match =>
            // Search the tail in place. Taking content.Substring(match.Index) here copied the rest
            // of the document once per open tag, which is quadratic: a 420 KB page with 18 <script>
            // tags allocated ~16 MB before parsing even began.
            closeRegex.IsMatch(content, match.Index) ? match.Value : string.Empty);
    }

    public static string SlackTidy(string content)
    {
        // Slack's escaping rules depend on whether the key characters appear in
        // next to word characters or not.
        content = SlackBoldCleaner().Replace(content, "*");
        content = SlackItalicCleaner().Replace(content, "_");

        return content;
    }
}
