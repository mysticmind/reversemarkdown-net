using System;
using System.Text.RegularExpressions;
using ReverseMarkdown.Helpers;


namespace ReverseMarkdown;

public static partial class Cleaner {
#if NET7_0_OR_GREATER
    [GeneratedRegex(@"\*(\s\*)+")]
    private static partial Regex SlackBoldCleaner { get; }
#else
    private static Regex SlackBoldCleaner { get; } = new(@"\*(\s\*)+", RegexOptions.Compiled);
#endif

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"_(\s_)+")]
    private static partial Regex SlackItalicCleaner { get; }
#else
    private static Regex SlackItalicCleaner { get; } = new(@"_(\s_)+", RegexOptions.Compiled);
#endif

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"[\u0020\u00A0]")]
    private static partial Regex NonBreakingSpaces { get; }
#else
    private static Regex NonBreakingSpaces { get; } = new(@"[\u0020\u00A0]", RegexOptions.Compiled);
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
        content = NonBreakingSpaces.Replace(content, " ");
        return content;
    }

    public static string PreTidy(string content, bool removeComments)
    {
        content = NormalizeSpaceChars(content);
        content = CleanTagBorders(content);

        return content;
    }

    public static string SlackTidy(string content)
    {
        // Slack's escaping rules depend on whether the key characters appear in
        // next to word characters or not.
        content = SlackBoldCleaner.Replace(content, "*");
        content = SlackItalicCleaner.Replace(content, "_");

        return content;
    }
}
